using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Inventario;

public record PrestamoDto(
    long Id, long BienId, string CodigoBien, string Bien, int Cantidad, string Destinatario, string? Actividad,
    DateOnly FechaSalida, DateOnly FechaDevolucionEsperada, DateOnly? FechaDevolucion, string? EstadoDevolucion,
    bool Vencido, string? RegistradoPor);

public class FiltroPrestamos : ConsultaPaginada
{
    /// <summary>"activos" (sin devolver), "vencidos" o "devueltos"; vacío = todos.</summary>
    public string? Estado { get; set; }
}

public record SolicitudPrestamo(
    [Range(1, 100_000, ErrorMessage = "La cantidad debe ser al menos 1.")] int Cantidad,
    [Required(ErrorMessage = "Indica a quién se presta.")][MaxLength(120, ErrorMessage = "El destinatario admite hasta 120 caracteres.")] string Destinatario,
    [MaxLength(120, ErrorMessage = "La actividad admite hasta 120 caracteres.")] string? Actividad,
    DateOnly? FechaSalida,
    DateOnly FechaDevolucionEsperada);

public record SolicitudDevolucion(
    DateOnly? FechaDevolucion,
    [Required(ErrorMessage = "Indica en qué estado volvió.")] string EstadoDevolucion);

/// <summary>
/// Préstamos de bienes para actividades y campamentos. Los registran administración, tesorería y dirigentes.
/// No se puede prestar más de lo disponible (cantidad total menos lo que ya está prestado).
/// </summary>
[ApiController]
[Route("api/inventario")]
[Authorize(Policy = Politicas.Inventario)]
public class PrestamosController(AppDbContext db, IUsuarioActual usuarioActual) : ControllerBase
{
    [HttpGet("prestamos")]
    public async Task<IActionResult> Listar([FromQuery] FiltroPrestamos filtro, CancellationToken ct)
    {
        if (filtro.Estado is not (null or "" or "activos" or "vencidos" or "devueltos"))
        {
            return BadRequest(ResponseData.Error("El estado debe ser activos, vencidos o devueltos."));
        }

        var hoy = Hoy();
        var consulta = filtro.Estado switch
        {
            "activos" => db.Prestamos.Where(p => p.FechaDevolucion == null),
            "vencidos" => db.Prestamos.Where(p => p.FechaDevolucion == null && p.FechaDevolucionEsperada < hoy),
            "devueltos" => db.Prestamos.Where(p => p.FechaDevolucion != null),
            _ => db.Prestamos,
        };
        if (filtro.BuscarNormalizado is { } texto)
        {
            consulta = consulta.Where(p => p.Destinatario.ToLower().Contains(texto) || p.Bien.Nombre.ToLower().Contains(texto)
                || p.Bien.Codigo.ToLower().Contains(texto) || (p.Actividad != null && p.Actividad.ToLower().Contains(texto)));
        }

        var total = await consulta.CountAsync(ct);
        var pagina = consulta
            .OrderBy(p => p.FechaDevolucion != null).ThenBy(p => p.FechaDevolucionEsperada)
            .Skip((filtro.Pagina - 1) * filtro.Tamano).Take(filtro.Tamano);
        var elementos = await ConsultarAsync(db, pagina, ct);
        return Ok(ResponseData.Ok(new Pagina<PrestamoDto>(elementos, total, filtro.Pagina, filtro.Tamano)));
    }

    [HttpPost("bienes/{bienId:long}/prestamos")]
    [Authorize(Policy = Politicas.InventarioEscritura)]
    public async Task<IActionResult> Prestar(long bienId, SolicitudPrestamo solicitud, CancellationToken ct)
    {
        // Bloquea la fila del bien hasta guardar: si llegan dos préstamos al mismo tiempo (doble clic, dos personas),
        // el segundo espera y ve lo que ya se prestó, así nunca se presta más de lo que hay.
        // Las pruebas usan una base en memoria, sin transacciones ni SQL, por eso solo se hace en PostgreSQL.
        var relacional = db.Database.IsRelational();
        await using var transaccion = relacional ? await db.Database.BeginTransactionAsync(ct) : null;
        if (relacional)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM bienes WHERE id = {bienId} FOR UPDATE", ct);
        }

        var bien = await db.Bienes.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bienId, ct);
        if (bien is null) return NotFound(ResponseData.Error("No se encontró el bien."));
        if (bien.DadoDeBaja) return Conflict(ResponseData.Error("El bien está dado de baja."));

        var salida = solicitud.FechaSalida ?? Hoy();
        if (salida > Hoy()) return BadRequest(ResponseData.Error("La fecha de salida no puede ser futura."));
        if (solicitud.FechaDevolucionEsperada < salida)
        {
            return BadRequest(ResponseData.Error("La devolución esperada no puede ser anterior a la salida."));
        }

        var disponible = bien.Cantidad - await db.Prestamos
            .Where(p => p.BienId == bienId && p.FechaDevolucion == null).SumAsync(p => p.Cantidad, ct);
        if (solicitud.Cantidad > disponible)
        {
            return Conflict(ResponseData.Error(disponible switch
            {
                <= 0 => "No hay unidades disponibles de este bien: todas están prestadas.",
                1 => "Solo hay 1 unidad disponible para prestar.",
                _ => $"Solo hay {disponible} unidades disponibles para prestar.",
            }));
        }

        var prestamo = new Prestamo
        {
            BienId = bienId,
            Cantidad = solicitud.Cantidad,
            Destinatario = solicitud.Destinatario.Trim(),
            Actividad = string.IsNullOrWhiteSpace(solicitud.Actividad) ? null : solicitud.Actividad.Trim(),
            FechaSalida = salida,
            FechaDevolucionEsperada = solicitud.FechaDevolucionEsperada,
            RegistradoPor = usuarioActual.Id!.Value,
        };
        db.Prestamos.Add(prestamo);
        await db.SaveChangesAsync(ct);
        if (transaccion is not null) await transaccion.CommitAsync(ct);
        return Ok(ResponseData.Ok(await UnoAsync(prestamo.Id, ct), "Préstamo registrado."));
    }

    /// <summary>
    /// Registra la devolución. Si algo vuelve "perdido", se descuenta de la cantidad del bien para que el disponible
    /// siga siendo real; el préstamo queda como registro de la pérdida.
    /// </summary>
    [HttpPost("prestamos/{id:long}/devolucion")]
    [Authorize(Policy = Politicas.InventarioEscritura)]
    public async Task<IActionResult> Devolver(long id, SolicitudDevolucion solicitud, CancellationToken ct)
    {
        var prestamo = await db.Prestamos.Include(p => p.Bien).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (prestamo is null) return NotFound(ResponseData.Error("No se encontró el préstamo."));
        if (prestamo.FechaDevolucion is not null) return Conflict(ResponseData.Error("Este préstamo ya fue devuelto."));
        if (!ValoresInventario.EstadosDevolucion.Contains(solicitud.EstadoDevolucion))
        {
            return BadRequest(ResponseData.Error("El estado de devolución no es válido."));
        }

        var fecha = solicitud.FechaDevolucion ?? Hoy();
        if (fecha > Hoy() || fecha < prestamo.FechaSalida)
        {
            return BadRequest(ResponseData.Error("La fecha de devolución no puede ser futura ni anterior a la salida."));
        }

        prestamo.FechaDevolucion = fecha;
        prestamo.EstadoDevolucion = solicitud.EstadoDevolucion;
        if (solicitud.EstadoDevolucion == "perdido") prestamo.Bien.Cantidad -= prestamo.Cantidad;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await UnoAsync(id, ct), "Devolución registrada."));
    }

    private async Task<PrestamoDto> UnoAsync(long id, CancellationToken ct) =>
        (await ConsultarAsync(db, db.Prestamos.Where(p => p.Id == id), ct)).Single();

    /// <summary>Convierte una consulta de préstamos a DTO (también la usa la ficha del bien).</summary>
    internal static async Task<List<PrestamoDto>> ConsultarAsync(AppDbContext db, IQueryable<Prestamo> consulta, CancellationToken ct)
    {
        var hoy = Hoy();
        return await consulta.AsNoTracking()
            .OrderByDescending(p => p.FechaSalida).ThenByDescending(p => p.Id)
            .Select(p => new PrestamoDto(p.Id, p.BienId, p.Bien.Codigo, p.Bien.Nombre, p.Cantidad, p.Destinatario, p.Actividad,
                p.FechaSalida, p.FechaDevolucionEsperada, p.FechaDevolucion, p.EstadoDevolucion,
                p.FechaDevolucion == null && p.FechaDevolucionEsperada < hoy,
                db.Users.Where(u => u.Id == p.RegistradoPor).Select(u => u.Nombre).FirstOrDefault()))
            .ToListAsync(ct);
    }

    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.Now);
}

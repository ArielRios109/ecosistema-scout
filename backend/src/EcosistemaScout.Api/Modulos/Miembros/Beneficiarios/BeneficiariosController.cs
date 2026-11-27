using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;

/// <summary>Estados del beneficiario y tipos de movimiento del historial (iguales a las restricciones CHECK).</summary>
public static class EstadosBeneficiario
{
    public const string Activo = "activo";
    public const string Retirado = "retirado";
}

public static class TiposHistorial
{
    public const string Ingreso = "ingreso";
    public const string CambioUnidad = "cambio_unidad";
    public const string CambioPatrulla = "cambio_patrulla";
    public const string Retiro = "retiro";
    public const string Reingreso = "reingreso";
}

public class FiltroBeneficiarios : ConsultaPaginada
{
    public long? UnidadId { get; set; }
    public long? PatrullaId { get; set; }
    /// <summary>activo o retirado; vacío = todos.</summary>
    public string? Estado { get; set; }
}

public record BeneficiarioResumen(
    long Id, string Rut, string Nombres, string Apellidos, DateOnly FechaNacimiento, int Edad,
    long UnidadId, string Unidad, long? PatrullaId, string? Patrulla, string Estado, DateOnly FechaIngreso);

public record BeneficiarioDetalle(
    long Id, string Rut, string Nombres, string Apellidos, DateOnly FechaNacimiento, int Edad, string? Direccion,
    long UnidadId, string Unidad, string Rama, long? PatrullaId, string? Patrulla, string Estado, DateOnly FechaIngreso,
    short? RegistroInstitucionalAnio);

public record MovimientoHistorial(
    long Id, string Tipo, DateOnly Fecha, string? UnidadOrigen, string? UnidadDestino, string? Detalle, string? Usuario);

public record SolicitudBeneficiario(
    [Required(ErrorMessage = "Ingresa el RUT.")][RutValido] string Rut,
    [Required(ErrorMessage = "Ingresa los nombres.")][MaxLength(80, ErrorMessage = "Los nombres admiten hasta 80 caracteres.")] string Nombres,
    [Required(ErrorMessage = "Ingresa los apellidos.")][MaxLength(80, ErrorMessage = "Los apellidos admiten hasta 80 caracteres.")] string Apellidos,
    DateOnly FechaNacimiento,
    [MaxLength(200, ErrorMessage = "La dirección admite hasta 200 caracteres.")] string? Direccion,
    long UnidadId,
    long? PatrullaId,
    DateOnly? FechaIngreso,
    [Range(1900, 2100, ErrorMessage = "El año de registro institucional no es válido.")] short? RegistroInstitucionalAnio);

public record SolicitudMovimiento(
    DateOnly? Fecha,
    [MaxLength(255, ErrorMessage = "El detalle admite hasta 255 caracteres.")] string? Detalle,
    long? UnidadId = null,
    long? PatrullaId = null);

/// <summary>
/// Beneficiarios (niños, niñas y jóvenes). Los gestionan administración y dirigentes.
/// Cada ingreso, cambio de unidad o patrulla, retiro y reingreso queda en el historial automáticamente.
/// Un beneficiario nunca se borra: se retira.
/// </summary>
[ApiController]
[Route("api/miembros/beneficiarios")]
[Authorize(Policy = Politicas.Miembros)]
public class BeneficiariosController(AppDbContext db, IUsuarioActual usuarioActual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] FiltroBeneficiarios filtro, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Estado) && filtro.Estado is not (EstadosBeneficiario.Activo or EstadosBeneficiario.Retirado))
        {
            return BadRequest(ResponseData.Error("El estado debe ser activo o retirado."));
        }

        var consulta = db.Beneficiarios.AsNoTracking();
        if (filtro.BuscarNormalizado is { } texto)
        {
            var rut = texto.Replace(".", "").Replace(" ", "");
            consulta = consulta.Where(b =>
                (b.Nombres + " " + b.Apellidos).ToLower().Contains(texto) || b.Rut.ToLower().Contains(rut));
        }
        if (filtro.UnidadId is { } unidadId) consulta = consulta.Where(b => b.UnidadId == unidadId);
        if (filtro.PatrullaId is { } patrullaId) consulta = consulta.Where(b => b.PatrullaId == patrullaId);
        if (!string.IsNullOrWhiteSpace(filtro.Estado)) consulta = consulta.Where(b => b.Estado == filtro.Estado);

        var pagina = await consulta
            .OrderBy(b => b.Apellidos).ThenBy(b => b.Nombres)
            .Select(b => new
            {
                b.Id, b.Rut, b.Nombres, b.Apellidos, b.FechaNacimiento, b.UnidadId, Unidad = b.Unidad.Nombre,
                b.PatrullaId, Patrulla = b.Patrulla != null ? b.Patrulla.Nombre : null, b.Estado, b.FechaIngreso,
            })
            .PaginarAsync(filtro, ct);

        var hoy = Hoy();
        var elementos = pagina.Elementos.Select(b => new BeneficiarioResumen(
            b.Id, Rut.Formatear(b.Rut), b.Nombres, b.Apellidos, b.FechaNacimiento, Edad(b.FechaNacimiento, hoy),
            b.UnidadId, b.Unidad, b.PatrullaId, b.Patrulla, b.Estado, b.FechaIngreso)).ToList();

        return Ok(ResponseData.Ok(new Pagina<BeneficiarioResumen>(elementos, pagina.Total, pagina.PaginaActual, pagina.Tamano)));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id, CancellationToken ct)
    {
        var detalle = await DetalleAsync(id, ct);
        return detalle is null ? NoEncontrado() : Ok(ResponseData.Ok(detalle));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(SolicitudBeneficiario solicitud, CancellationToken ct)
    {
        var rut = Rut.Normalizar(solicitud.Rut);
        if (ValidarFechas(solicitud) is { } errorFechas) return errorFechas;
        if (await db.Beneficiarios.AnyAsync(b => b.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe un beneficiario con ese RUT."));
        }
        if (await ValidarUbicacionAsync(solicitud.UnidadId, solicitud.PatrullaId, ct) is { } errorUbicacion) return errorUbicacion;

        var beneficiario = new Beneficiario
        {
            Rut = rut,
            Nombres = solicitud.Nombres.Trim(),
            Apellidos = solicitud.Apellidos.Trim(),
            FechaNacimiento = solicitud.FechaNacimiento,
            Direccion = Limpio(solicitud.Direccion),
            UnidadId = solicitud.UnidadId,
            PatrullaId = solicitud.PatrullaId,
            FechaIngreso = solicitud.FechaIngreso ?? Hoy(),
            RegistroInstitucionalAnio = solicitud.RegistroInstitucionalAnio,
            Estado = EstadosBeneficiario.Activo,
        };
        db.Beneficiarios.Add(beneficiario);
        Registrar(beneficiario, TiposHistorial.Ingreso, beneficiario.FechaIngreso, null, beneficiario.UnidadId, null);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Obtener), new { id = beneficiario.Id },
            ResponseData.Ok(await DetalleAsync(beneficiario.Id, ct), "Beneficiario registrado."));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Editar(long id, SolicitudBeneficiario solicitud, CancellationToken ct)
    {
        var beneficiario = await db.Beneficiarios.FindAsync([id], ct);
        if (beneficiario is null) return NoEncontrado();

        var rut = Rut.Normalizar(solicitud.Rut);
        if (ValidarFechas(solicitud) is { } errorFechas) return errorFechas;
        if (await db.Beneficiarios.AnyAsync(b => b.Id != id && b.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe otro beneficiario con ese RUT."));
        }

        var cambiaUnidad = beneficiario.UnidadId != solicitud.UnidadId;
        var cambiaPatrulla = beneficiario.PatrullaId != solicitud.PatrullaId;
        if ((cambiaUnidad || cambiaPatrulla) && beneficiario.Estado == EstadosBeneficiario.Retirado)
        {
            return Conflict(ResponseData.Error("El beneficiario está retirado. Reingrésalo para asignarle otra unidad o patrulla."));
        }
        if ((cambiaUnidad || cambiaPatrulla)
            && await ValidarUbicacionAsync(solicitud.UnidadId, solicitud.PatrullaId, ct) is { } errorUbicacion)
        {
            return errorUbicacion;
        }

        if (cambiaUnidad)
        {
            Registrar(beneficiario, TiposHistorial.CambioUnidad, Hoy(), beneficiario.UnidadId, solicitud.UnidadId, null);
        }
        else if (cambiaPatrulla)
        {
            var nombres = await db.Patrullas.Where(p => p.Id == beneficiario.PatrullaId || p.Id == solicitud.PatrullaId)
                .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);
            var desde = beneficiario.PatrullaId is { } a && nombres.TryGetValue(a, out var na) ? na : "sin patrulla";
            var hacia = solicitud.PatrullaId is { } b && nombres.TryGetValue(b, out var nb) ? nb : "sin patrulla";
            Registrar(beneficiario, TiposHistorial.CambioPatrulla, Hoy(), beneficiario.UnidadId, beneficiario.UnidadId,
                $"De {desde} a {hacia}.");
        }

        beneficiario.Rut = rut;
        beneficiario.Nombres = solicitud.Nombres.Trim();
        beneficiario.Apellidos = solicitud.Apellidos.Trim();
        beneficiario.FechaNacimiento = solicitud.FechaNacimiento;
        beneficiario.Direccion = Limpio(solicitud.Direccion);
        beneficiario.UnidadId = solicitud.UnidadId;
        beneficiario.PatrullaId = solicitud.PatrullaId;
        if (solicitud.FechaIngreso is { } ingreso) beneficiario.FechaIngreso = ingreso;
        beneficiario.RegistroInstitucionalAnio = solicitud.RegistroInstitucionalAnio;
        await db.SaveChangesAsync(ct);

        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Datos del beneficiario actualizados."));
    }

    /// <summary>Retira al beneficiario del grupo (sus datos se conservan).</summary>
    [HttpPost("{id:long}/retirar")]
    public async Task<IActionResult> Retirar(long id, SolicitudMovimiento solicitud, CancellationToken ct)
    {
        var beneficiario = await db.Beneficiarios.FindAsync([id], ct);
        if (beneficiario is null) return NoEncontrado();
        if (beneficiario.Estado == EstadosBeneficiario.Retirado)
        {
            return Conflict(ResponseData.Error("El beneficiario ya está retirado."));
        }

        var fecha = solicitud.Fecha ?? Hoy();
        if (fecha > Hoy()) return BadRequest(ResponseData.Error("La fecha de retiro no puede ser futura."));

        beneficiario.Estado = EstadosBeneficiario.Retirado;
        Registrar(beneficiario, TiposHistorial.Retiro, fecha, beneficiario.UnidadId, null, Limpio(solicitud.Detalle));
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Beneficiario retirado."));
    }

    /// <summary>Reingresa a un beneficiario retirado, opcionalmente en otra unidad o patrulla.</summary>
    [HttpPost("{id:long}/reingresar")]
    public async Task<IActionResult> Reingresar(long id, SolicitudMovimiento solicitud, CancellationToken ct)
    {
        var beneficiario = await db.Beneficiarios.FindAsync([id], ct);
        if (beneficiario is null) return NoEncontrado();
        if (beneficiario.Estado == EstadosBeneficiario.Activo)
        {
            return Conflict(ResponseData.Error("El beneficiario ya está activo."));
        }

        var fecha = solicitud.Fecha ?? Hoy();
        if (fecha > Hoy()) return BadRequest(ResponseData.Error("La fecha de reingreso no puede ser futura."));

        var unidadId = solicitud.UnidadId ?? beneficiario.UnidadId;
        var patrullaId = solicitud.UnidadId is null ? solicitud.PatrullaId ?? beneficiario.PatrullaId : solicitud.PatrullaId;
        if (await ValidarUbicacionAsync(unidadId, patrullaId, ct) is { } errorUbicacion) return errorUbicacion;

        Registrar(beneficiario, TiposHistorial.Reingreso, fecha, beneficiario.UnidadId, unidadId, Limpio(solicitud.Detalle));
        beneficiario.Estado = EstadosBeneficiario.Activo;
        beneficiario.UnidadId = unidadId;
        beneficiario.PatrullaId = patrullaId;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Beneficiario reingresado."));
    }

    /// <summary>Movimientos del beneficiario, del más reciente al más antiguo.</summary>
    [HttpGet("{id:long}/historial")]
    public async Task<IActionResult> Historial(long id, CancellationToken ct)
    {
        if (!await db.Beneficiarios.AnyAsync(b => b.Id == id, ct)) return NoEncontrado();

        var movimientos = await db.HistorialBeneficiario.AsNoTracking()
            .Where(h => h.BeneficiarioId == id)
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id)
            .ToListAsync(ct);

        var idsUnidades = movimientos.SelectMany(m => new[] { m.UnidadOrigenId, m.UnidadDestinoId })
            .OfType<long>().Distinct().ToList();
        var unidades = await db.Unidades.Where(u => idsUnidades.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nombre, ct);
        var idsUsuarios = movimientos.Select(m => m.UsuarioId).OfType<long>().Distinct().ToList();
        var usuarios = await db.Users.Where(u => idsUsuarios.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nombre, ct);

        var resultado = movimientos.Select(m => new MovimientoHistorial(
            m.Id, m.Tipo, m.Fecha,
            m.UnidadOrigenId is { } o ? unidades.GetValueOrDefault(o) : null,
            m.UnidadDestinoId is { } d ? unidades.GetValueOrDefault(d) : null,
            m.Detalle,
            m.UsuarioId is { } u ? usuarios.GetValueOrDefault(u) : null)).ToList();

        return Ok(ResponseData.Ok<IReadOnlyList<MovimientoHistorial>>(resultado));
    }

    private void Registrar(Beneficiario beneficiario, string tipo, DateOnly fecha, long? origen, long? destino, string? detalle) =>
        db.HistorialBeneficiario.Add(new HistorialBeneficiario
        {
            Beneficiario = beneficiario,
            Tipo = tipo,
            Fecha = fecha,
            UnidadOrigenId = origen,
            UnidadDestinoId = destino,
            Detalle = detalle,
            UsuarioId = usuarioActual.Id,
        });

    private async Task<BeneficiarioDetalle?> DetalleAsync(long id, CancellationToken ct)
    {
        var b = await db.Beneficiarios.AsNoTracking().Include(x => x.Unidad).Include(x => x.Patrulla)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return b is null
            ? null
            : new BeneficiarioDetalle(b.Id, Rut.Formatear(b.Rut), b.Nombres, b.Apellidos, b.FechaNacimiento,
                Edad(b.FechaNacimiento, Hoy()), b.Direccion, b.UnidadId, b.Unidad.Nombre, b.Unidad.Rama,
                b.PatrullaId, b.Patrulla?.Nombre, b.Estado, b.FechaIngreso, b.RegistroInstitucionalAnio);
    }

    /// <summary>La unidad debe existir y estar activa; la patrulla (opcional) debe ser de esa unidad y estar activa.</summary>
    private async Task<IActionResult?> ValidarUbicacionAsync(long unidadId, long? patrullaId, CancellationToken ct)
    {
        var unidad = await db.Unidades.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unidadId, ct);
        if (unidad is null) return BadRequest(ResponseData.Error("Selecciona una unidad válida."));
        if (!unidad.Activa) return Conflict(ResponseData.Error("La unidad seleccionada está desactivada."));

        if (patrullaId is { } idPatrulla)
        {
            var patrulla = await db.Patrullas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == idPatrulla, ct);
            if (patrulla is null || patrulla.UnidadId != unidadId)
            {
                return BadRequest(ResponseData.Error("La patrulla no pertenece a la unidad seleccionada."));
            }
            if (!patrulla.Activa) return Conflict(ResponseData.Error("La patrulla seleccionada está desactivada."));
        }
        return null;
    }

    private BadRequestObjectResult? ValidarFechas(SolicitudBeneficiario solicitud)
    {
        var hoy = Hoy();
        if (solicitud.FechaNacimiento == default || solicitud.FechaNacimiento > hoy || solicitud.FechaNacimiento.Year < 1900)
        {
            return BadRequest(ResponseData.Error("Ingresa una fecha de nacimiento válida."));
        }
        if (solicitud.FechaIngreso is { } ingreso && (ingreso > hoy || ingreso < solicitud.FechaNacimiento))
        {
            return BadRequest(ResponseData.Error("La fecha de ingreso no puede ser futura ni anterior al nacimiento."));
        }
        return null;
    }

    private NotFoundObjectResult NoEncontrado() => NotFound(ResponseData.Error("No se encontró el beneficiario."));

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.Now);

    public static int Edad(DateOnly nacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - nacimiento.Year;
        if (hoy < nacimiento.AddYears(edad)) edad--;
        return Math.Max(0, edad);
    }
}

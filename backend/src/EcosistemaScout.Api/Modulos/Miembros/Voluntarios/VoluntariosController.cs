using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Miembros.Voluntarios;

public record CargoDto(long Id, string Cargo, long? UnidadId, string? Unidad, DateOnly FechaInicio, DateOnly? FechaTermino, bool Vigente);

public record VoluntarioResumen(
    long Id, string Rut, string Nombres, string Apellidos, string? Email, string? Telefono, bool TieneCuenta,
    IReadOnlyList<CargoDto> CargosVigentes);

public record VoluntarioDetalle(
    long Id, string Rut, string Nombres, string Apellidos, string? Email, string? Telefono, bool TieneCuenta,
    IReadOnlyList<CargoDto> Cargos);

public class FiltroVoluntarios : ConsultaPaginada
{
    /// <summary>Solo voluntarios con un cargo vigente en esa unidad.</summary>
    public long? UnidadId { get; set; }
    /// <summary>true = solo con cargo vigente; false = solo sin cargo vigente; vacío = todos.</summary>
    public bool? ConCargo { get; set; }
}

public record SolicitudVoluntario(
    [Required(ErrorMessage = "Ingresa el RUT.")][RutValido] string Rut,
    [Required(ErrorMessage = "Ingresa los nombres.")][MaxLength(80, ErrorMessage = "Los nombres admiten hasta 80 caracteres.")] string Nombres,
    [Required(ErrorMessage = "Ingresa los apellidos.")][MaxLength(80, ErrorMessage = "Los apellidos admiten hasta 80 caracteres.")] string Apellidos,
    [EmailAddress(ErrorMessage = "El correo no es válido.")][MaxLength(120)] string? Email,
    [MaxLength(20, ErrorMessage = "El teléfono admite hasta 20 caracteres.")] string? Telefono);

public record SolicitudCargo(
    [Required(ErrorMessage = "Indica el cargo.")][MaxLength(60, ErrorMessage = "El cargo admite hasta 60 caracteres.")] string Cargo,
    long? UnidadId,
    DateOnly FechaInicio,
    DateOnly? FechaTermino);

public record SolicitudTerminarCargo(DateOnly? FechaTermino);

/// <summary>
/// Voluntarios adultos del grupo (dirigentes, guiadoras, apoyo) y sus cargos con fechas de inicio y término.
/// Un cargo sin fecha de término, o con término futuro, está vigente. Los cargos terminados se conservan como historial.
/// </summary>
[ApiController]
[Route("api/miembros/voluntarios")]
[Authorize(Policy = Politicas.Miembros)]
public class VoluntariosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ResponseData<Pagina<VoluntarioResumen>>> Listar([FromQuery] FiltroVoluntarios filtro, CancellationToken ct)
    {
        var hoy = Hoy();
        var consulta = db.Voluntarios.AsNoTracking();
        if (filtro.BuscarNormalizado is { } texto)
        {
            var rut = texto.Replace(".", "").Replace(" ", "");
            consulta = consulta.Where(v =>
                (v.Nombres + " " + v.Apellidos).ToLower().Contains(texto)
                || (v.Email != null && v.Email.ToLower().Contains(texto))
                || v.Rut.ToLower().Contains(rut)
                || v.Cargos.Any(c => c.Cargo.ToLower().Contains(texto)));
        }
        if (filtro.UnidadId is { } unidadId)
        {
            consulta = consulta.Where(v => v.Cargos.Any(c => c.UnidadId == unidadId && (c.FechaTermino == null || c.FechaTermino >= hoy)));
        }
        if (filtro.ConCargo is { } conCargo)
        {
            consulta = consulta.Where(v => v.Cargos.Any(c => c.FechaTermino == null || c.FechaTermino >= hoy) == conCargo);
        }

        var pagina = await consulta.OrderBy(v => v.Apellidos).ThenBy(v => v.Nombres).PaginarAsync(filtro, ct);
        var ids = pagina.Elementos.Select(v => v.Id).ToList();
        var cargos = (await CargosAsync(c => ids.Contains(c.VoluntarioId) && (c.FechaTermino == null || c.FechaTermino >= hoy), ct))
            .ToLookup(c => c.VoluntarioId, c => c.Dto);

        return ResponseData.Ok(new Pagina<VoluntarioResumen>(
            pagina.Elementos.Select(v => new VoluntarioResumen(v.Id, Rut.Formatear(v.Rut), v.Nombres, v.Apellidos, v.Email,
                v.Telefono, v.UsuarioId != null, cargos[v.Id].ToList())).ToList(),
            pagina.Total, pagina.PaginaActual, pagina.Tamano));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id, CancellationToken ct)
    {
        var detalle = await DetalleAsync(id, ct);
        return detalle is null ? NoEncontrado() : Ok(ResponseData.Ok(detalle));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(SolicitudVoluntario solicitud, CancellationToken ct)
    {
        var rut = Rut.Normalizar(solicitud.Rut);
        if (await db.Voluntarios.AnyAsync(v => v.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe un voluntario con ese RUT."));
        }

        var voluntario = new Voluntario();
        Copiar(solicitud, voluntario, rut);
        db.Voluntarios.Add(voluntario);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = voluntario.Id },
            ResponseData.Ok(await DetalleAsync(voluntario.Id, ct), "Voluntario registrado."));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Editar(long id, SolicitudVoluntario solicitud, CancellationToken ct)
    {
        var voluntario = await db.Voluntarios.FindAsync([id], ct);
        if (voluntario is null) return NoEncontrado();

        var rut = Rut.Normalizar(solicitud.Rut);
        if (await db.Voluntarios.AnyAsync(v => v.Id != id && v.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe otro voluntario con ese RUT."));
        }

        Copiar(solicitud, voluntario, rut);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Datos del voluntario actualizados."));
    }

    /// <summary>Solo se elimina un voluntario sin cargos (los que tuvieron cargos se conservan como historial).</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id, CancellationToken ct)
    {
        var voluntario = await db.Voluntarios.FindAsync([id], ct);
        if (voluntario is null) return NoEncontrado();
        if (await db.CargosVoluntario.AnyAsync(c => c.VoluntarioId == id, ct))
        {
            return Conflict(ResponseData.Error("El voluntario tiene cargos registrados y se conserva como historial. Termina sus cargos vigentes."));
        }

        db.Voluntarios.Remove(voluntario);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok<object?>(null, "Voluntario eliminado."));
    }

    // ----- Cargos -----

    [HttpPost("{id:long}/cargos")]
    public async Task<IActionResult> AgregarCargo(long id, SolicitudCargo solicitud, CancellationToken ct)
    {
        if (!await db.Voluntarios.AnyAsync(v => v.Id == id, ct)) return NoEncontrado();
        if (await ValidarCargoAsync(solicitud, ct) is { } error) return error;

        db.CargosVoluntario.Add(new CargoVoluntario
        {
            VoluntarioId = id,
            Cargo = solicitud.Cargo.Trim(),
            UnidadId = solicitud.UnidadId,
            FechaInicio = solicitud.FechaInicio,
            FechaTermino = solicitud.FechaTermino,
        });
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Cargo asignado."));
    }

    [HttpPut("{id:long}/cargos/{cargoId:long}")]
    public async Task<IActionResult> EditarCargo(long id, long cargoId, SolicitudCargo solicitud, CancellationToken ct)
    {
        var cargo = await db.CargosVoluntario.FirstOrDefaultAsync(c => c.Id == cargoId && c.VoluntarioId == id, ct);
        if (cargo is null) return CargoNoEncontrado();
        if (await ValidarCargoAsync(solicitud, ct, cargo.UnidadId) is { } error) return error;

        cargo.Cargo = solicitud.Cargo.Trim();
        cargo.UnidadId = solicitud.UnidadId;
        cargo.FechaInicio = solicitud.FechaInicio;
        cargo.FechaTermino = solicitud.FechaTermino;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Cargo actualizado."));
    }

    /// <summary>Termina un cargo vigente (por defecto, hoy). Queda en el historial del voluntario.</summary>
    [HttpPost("{id:long}/cargos/{cargoId:long}/terminar")]
    public async Task<IActionResult> TerminarCargo(long id, long cargoId, SolicitudTerminarCargo solicitud, CancellationToken ct)
    {
        var cargo = await db.CargosVoluntario.FirstOrDefaultAsync(c => c.Id == cargoId && c.VoluntarioId == id, ct);
        if (cargo is null) return CargoNoEncontrado();
        if (cargo.FechaTermino is { } termino && termino < Hoy())
        {
            return Conflict(ResponseData.Error("El cargo ya está terminado."));
        }

        var fecha = solicitud.FechaTermino ?? Hoy();
        if (fecha < cargo.FechaInicio)
        {
            return BadRequest(ResponseData.Error("La fecha de término no puede ser anterior al inicio del cargo."));
        }

        cargo.FechaTermino = fecha;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Cargo terminado."));
    }

    [HttpDelete("{id:long}/cargos/{cargoId:long}")]
    public async Task<IActionResult> EliminarCargo(long id, long cargoId, CancellationToken ct)
    {
        var cargo = await db.CargosVoluntario.FirstOrDefaultAsync(c => c.Id == cargoId && c.VoluntarioId == id, ct);
        if (cargo is null) return CargoNoEncontrado();

        db.CargosVoluntario.Remove(cargo);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Cargo eliminado."));
    }

    /// <summary>Fechas coherentes; la unidad (opcional, para cargos de unidad) debe existir y, si cambia, estar activa.</summary>
    private async Task<IActionResult?> ValidarCargoAsync(SolicitudCargo solicitud, CancellationToken ct, long? unidadActual = null)
    {
        if (solicitud.FechaInicio == default || solicitud.FechaInicio > Hoy())
        {
            return BadRequest(ResponseData.Error("La fecha de inicio no puede quedar vacía ni ser futura."));
        }
        if (solicitud.FechaTermino is { } termino && termino < solicitud.FechaInicio)
        {
            return BadRequest(ResponseData.Error("La fecha de término no puede ser anterior al inicio."));
        }
        if (solicitud.UnidadId is { } unidadId)
        {
            var unidad = await db.Unidades.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unidadId, ct);
            if (unidad is null) return BadRequest(ResponseData.Error("Selecciona una unidad válida."));
            if (!unidad.Activa && unidadId != unidadActual) return Conflict(ResponseData.Error("La unidad seleccionada está desactivada."));
        }
        return null;
    }

    private async Task<List<(long VoluntarioId, CargoDto Dto)>> CargosAsync(
        System.Linq.Expressions.Expression<Func<CargoVoluntario, bool>> condicion, CancellationToken ct)
    {
        var hoy = Hoy();
        var cargos = await db.CargosVoluntario.AsNoTracking()
            .Where(condicion)
            .OrderBy(c => c.FechaTermino != null).ThenByDescending(c => c.FechaInicio)
            .Select(c => new { c.VoluntarioId, c.Id, c.Cargo, c.UnidadId, Unidad = c.Unidad != null ? c.Unidad.Nombre : null, c.FechaInicio, c.FechaTermino })
            .ToListAsync(ct);
        return cargos.Select(c => (c.VoluntarioId, new CargoDto(c.Id, c.Cargo, c.UnidadId, c.Unidad, c.FechaInicio, c.FechaTermino,
            c.FechaTermino is null || c.FechaTermino >= hoy))).ToList();
    }

    private async Task<VoluntarioDetalle?> DetalleAsync(long id, CancellationToken ct)
    {
        var v = await db.Voluntarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return null;

        var cargos = (await CargosAsync(c => c.VoluntarioId == id, ct)).Select(c => c.Dto).ToList();
        return new VoluntarioDetalle(v.Id, Rut.Formatear(v.Rut), v.Nombres, v.Apellidos, v.Email, v.Telefono, v.UsuarioId != null, cargos);
    }

    private static void Copiar(SolicitudVoluntario solicitud, Voluntario voluntario, string rut)
    {
        voluntario.Rut = rut;
        voluntario.Nombres = solicitud.Nombres.Trim();
        voluntario.Apellidos = solicitud.Apellidos.Trim();
        voluntario.Email = string.IsNullOrWhiteSpace(solicitud.Email) ? null : solicitud.Email.Trim().ToLowerInvariant();
        voluntario.Telefono = string.IsNullOrWhiteSpace(solicitud.Telefono) ? null : solicitud.Telefono.Trim();
    }

    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.Now);

    private NotFoundObjectResult NoEncontrado() => NotFound(ResponseData.Error("No se encontró el voluntario."));
    private NotFoundObjectResult CargoNoEncontrado() => NotFound(ResponseData.Error("No se encontró el cargo."));
}

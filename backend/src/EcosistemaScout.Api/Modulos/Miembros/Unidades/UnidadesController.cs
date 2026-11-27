using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Miembros.Unidades;

/// <summary>Cómo se llaman los subgrupos de una rama y cuántos integrantes admite cada uno (null = sin límite).</summary>
public record TipoSubgrupo(string Nombre, string Plural, int? Maximo);

/// <summary>Ramas válidas (igual que la restricción CHECK de la tabla unidades) y sus reglas según el PGS.</summary>
public static class Ramas
{
    /// <summary>Máximo de beneficiarios activos por unidad.</summary>
    public const int MaximoPorUnidad = 32;

    private static readonly Dictionary<string, TipoSubgrupo> Subgrupos = new()
    {
        ["golondrinas"] = new("seisena", "seisenas", 6),
        ["lobatos"] = new("seisena", "seisenas", 6),
        ["guias"] = new("patrulla", "patrullas", 8),
        ["scouts"] = new("patrulla", "patrullas", 8),
        ["pioneros"] = new("equipo", "equipos", null),
        ["caminantes"] = new("comunidad", "comunidades", null),
    };

    public static IReadOnlyList<string> Todas { get; } = [.. Subgrupos.Keys];

    public static TipoSubgrupo Subgrupo(string rama) => Subgrupos.GetValueOrDefault(rama) ?? new("patrulla", "patrullas", null);
}

public record PatrullaDto(long Id, long UnidadId, string Nombre, bool Activa, int Beneficiarios);

public record UnidadDto(long Id, string Nombre, string Rama, bool Activa, int Beneficiarios, IReadOnlyList<PatrullaDto> Patrullas)
{
    // Reglas de la rama, para que el frontend muestre los cupos sin repetirlas.
    public int Maximo => Ramas.MaximoPorUnidad;
    public TipoSubgrupo Subgrupo => Ramas.Subgrupo(Rama);
}

public record SolicitudUnidad(
    [Required(ErrorMessage = "Ingresa el nombre de la unidad.")][MaxLength(80, ErrorMessage = "El nombre admite hasta 80 caracteres.")] string Nombre,
    [Required(ErrorMessage = "Selecciona la rama.")] string Rama);

public record SolicitudPatrulla(
    [Required(ErrorMessage = "Ingresa el nombre de la patrulla.")][MaxLength(80, ErrorMessage = "El nombre admite hasta 80 caracteres.")] string Nombre);

/// <summary>
/// Unidades del grupo y sus patrullas. Las ven administración y dirigentes; las unidades las administra
/// el rol administrador y las patrullas también los dirigentes. Nada con beneficiarios se borra: se desactiva.
/// </summary>
[ApiController]
[Route("api/miembros")]
[Authorize(Policy = Politicas.Miembros)]
public class UnidadesController(AppDbContext db) : ControllerBase
{
    /// <summary>Unidades con sus patrullas y la cantidad de beneficiarios activos.</summary>
    [HttpGet("unidades")]
    public async Task<ResponseData<IReadOnlyList<UnidadDto>>> Listar([FromQuery] bool incluirInactivas = false, CancellationToken ct = default)
    {
        var consulta = db.Unidades.AsNoTracking();
        if (!incluirInactivas) consulta = consulta.Where(u => u.Activa);

        var unidades = await consulta.OrderBy(u => u.Nombre).Select(Proyeccion).ToListAsync(ct);
        return ResponseData.Ok<IReadOnlyList<UnidadDto>>(unidades);
    }

    [HttpGet("unidades/{id:long}")]
    public async Task<IActionResult> Obtener(long id, CancellationToken ct)
    {
        var unidad = await db.Unidades.AsNoTracking().Where(u => u.Id == id).Select(Proyeccion).FirstOrDefaultAsync(ct);
        return unidad is null ? UnidadNoEncontrada() : Ok(ResponseData.Ok(unidad));
    }

    [HttpPost("unidades")]
    [Authorize(Policy = Politicas.Administracion)]
    public async Task<IActionResult> Crear(SolicitudUnidad solicitud, CancellationToken ct)
    {
        if (ValidarRama(solicitud.Rama) is { } error) return error;
        if (await db.Unidades.AnyAsync(u => u.Nombre.ToLower() == solicitud.Nombre.Trim().ToLower(), ct))
        {
            return Conflict(ResponseData.Error("Ya existe una unidad con ese nombre."));
        }

        var unidad = new Unidad { Nombre = solicitud.Nombre.Trim(), Rama = solicitud.Rama };
        db.Unidades.Add(unidad);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = unidad.Id }, ResponseData.Ok(await DtoAsync(unidad.Id, ct), "Unidad creada."));
    }

    [HttpPut("unidades/{id:long}")]
    [Authorize(Policy = Politicas.Administracion)]
    public async Task<IActionResult> Editar(long id, SolicitudUnidad solicitud, CancellationToken ct)
    {
        if (ValidarRama(solicitud.Rama) is { } error) return error;
        var unidad = await db.Unidades.FindAsync([id], ct);
        if (unidad is null) return UnidadNoEncontrada();
        if (await db.Unidades.AnyAsync(u => u.Id != id && u.Nombre.ToLower() == solicitud.Nombre.Trim().ToLower(), ct))
        {
            return Conflict(ResponseData.Error("Ya existe otra unidad con ese nombre."));
        }

        unidad.Nombre = solicitud.Nombre.Trim();
        unidad.Rama = solicitud.Rama;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DtoAsync(id, ct), "Unidad actualizada."));
    }

    /// <summary>Desactiva la unidad; solo si ya no tiene beneficiarios activos (hay que cambiarlos de unidad antes).</summary>
    [HttpPost("unidades/{id:long}/desactivar")]
    [Authorize(Policy = Politicas.Administracion)]
    public async Task<IActionResult> Desactivar(long id, CancellationToken ct)
    {
        var unidad = await db.Unidades.FindAsync([id], ct);
        if (unidad is null) return UnidadNoEncontrada();
        if (await db.Beneficiarios.AnyAsync(b => b.UnidadId == id && b.Estado == "activo", ct))
        {
            return Conflict(ResponseData.Error("La unidad tiene beneficiarios activos. Cámbialos de unidad antes de desactivarla."));
        }

        unidad.Activa = false;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DtoAsync(id, ct), "Unidad desactivada."));
    }

    [HttpPost("unidades/{id:long}/activar")]
    [Authorize(Policy = Politicas.Administracion)]
    public async Task<IActionResult> Activar(long id, CancellationToken ct)
    {
        var unidad = await db.Unidades.FindAsync([id], ct);
        if (unidad is null) return UnidadNoEncontrada();
        unidad.Activa = true;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DtoAsync(id, ct), "Unidad activada."));
    }

    /// <summary>Elimina una unidad creada por error: solo si nunca tuvo beneficiarios ni patrullas.</summary>
    [HttpDelete("unidades/{id:long}")]
    [Authorize(Policy = Politicas.Administracion)]
    public async Task<IActionResult> Eliminar(long id, CancellationToken ct)
    {
        var unidad = await db.Unidades.FindAsync([id], ct);
        if (unidad is null) return UnidadNoEncontrada();
        var enUso = await db.Beneficiarios.AnyAsync(b => b.UnidadId == id, ct)
                    || await db.Patrullas.AnyAsync(p => p.UnidadId == id, ct)
                    || await db.HistorialBeneficiario.AnyAsync(h => h.UnidadOrigenId == id || h.UnidadDestinoId == id, ct)
                    || await db.CargosVoluntario.AnyAsync(c => c.UnidadId == id, ct);
        if (enUso)
        {
            return Conflict(ResponseData.Error("La unidad tiene datos asociados y no se puede eliminar. Puedes desactivarla."));
        }

        db.Unidades.Remove(unidad);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(id, "Unidad eliminada."));
    }

    // ----- Patrullas -----

    [HttpPost("unidades/{unidadId:long}/patrullas")]
    public async Task<IActionResult> CrearPatrulla(long unidadId, SolicitudPatrulla solicitud, CancellationToken ct)
    {
        var rama = await db.Unidades.Where(u => u.Id == unidadId).Select(u => u.Rama).FirstOrDefaultAsync(ct);
        if (rama is null) return UnidadNoEncontrada();
        var subgrupo = Ramas.Subgrupo(rama);
        if (await NombrePatrullaUsadoAsync(unidadId, solicitud.Nombre, null, ct))
        {
            return Conflict(ResponseData.Error($"Ya existe una {subgrupo.Nombre} con ese nombre en la unidad."));
        }

        var patrulla = new Patrulla { UnidadId = unidadId, Nombre = solicitud.Nombre.Trim() };
        db.Patrullas.Add(patrulla);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await PatrullaDtoAsync(patrulla.Id, ct), $"{char.ToUpper(subgrupo.Nombre[0])}{subgrupo.Nombre[1..]} creada."));
    }

    [HttpPut("patrullas/{id:long}")]
    public async Task<IActionResult> EditarPatrulla(long id, SolicitudPatrulla solicitud, CancellationToken ct)
    {
        var patrulla = await db.Patrullas.FindAsync([id], ct);
        if (patrulla is null) return PatrullaNoEncontrada();
        if (await NombrePatrullaUsadoAsync(patrulla.UnidadId, solicitud.Nombre, id, ct))
        {
            return Conflict(ResponseData.Error("Ya existe otra patrulla con ese nombre en la unidad."));
        }

        patrulla.Nombre = solicitud.Nombre.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await PatrullaDtoAsync(id, ct), "Patrulla actualizada."));
    }

    /// <summary>Desactiva la patrulla y deja sin patrulla a sus beneficiarios (siguen en la unidad).</summary>
    [HttpPost("patrullas/{id:long}/desactivar")]
    public async Task<IActionResult> DesactivarPatrulla(long id, CancellationToken ct)
    {
        var patrulla = await db.Patrullas.FindAsync([id], ct);
        if (patrulla is null) return PatrullaNoEncontrada();
        if (await db.Beneficiarios.AnyAsync(b => b.PatrullaId == id && b.Estado == "activo", ct))
        {
            return Conflict(ResponseData.Error("La patrulla tiene beneficiarios activos. Cámbialos de patrulla antes de desactivarla."));
        }

        patrulla.Activa = false;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await PatrullaDtoAsync(id, ct), "Patrulla desactivada."));
    }

    [HttpPost("patrullas/{id:long}/activar")]
    public async Task<IActionResult> ActivarPatrulla(long id, CancellationToken ct)
    {
        var patrulla = await db.Patrullas.FindAsync([id], ct);
        if (patrulla is null) return PatrullaNoEncontrada();
        patrulla.Activa = true;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await PatrullaDtoAsync(id, ct), "Patrulla activada."));
    }

    [HttpDelete("patrullas/{id:long}")]
    public async Task<IActionResult> EliminarPatrulla(long id, CancellationToken ct)
    {
        var patrulla = await db.Patrullas.FindAsync([id], ct);
        if (patrulla is null) return PatrullaNoEncontrada();
        if (await db.Beneficiarios.AnyAsync(b => b.PatrullaId == id, ct))
        {
            return Conflict(ResponseData.Error("La patrulla tiene beneficiarios asociados y no se puede eliminar. Puedes desactivarla."));
        }

        db.Patrullas.Remove(patrulla);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(id, "Patrulla eliminada."));
    }

    // ----- Ayudas -----

    private static readonly System.Linq.Expressions.Expression<Func<Unidad, UnidadDto>> Proyeccion = u => new UnidadDto(
        u.Id, u.Nombre, u.Rama, u.Activa,
        u.Beneficiarios.Count(b => b.Estado == "activo"),
        u.Patrullas.OrderBy(p => p.Nombre).Select(p => new PatrullaDto(
            p.Id, p.UnidadId, p.Nombre, p.Activa,
            u.Beneficiarios.Count(b => b.PatrullaId == p.Id && b.Estado == "activo"))).ToList());

    private async Task<UnidadDto> DtoAsync(long id, CancellationToken ct) =>
        await db.Unidades.AsNoTracking().Where(u => u.Id == id).Select(Proyeccion).FirstAsync(ct);

    private async Task<PatrullaDto> PatrullaDtoAsync(long id, CancellationToken ct) =>
        await db.Patrullas.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new PatrullaDto(p.Id, p.UnidadId, p.Nombre, p.Activa,
                db.Beneficiarios.Count(b => b.PatrullaId == p.Id && b.Estado == "activo")))
            .FirstAsync(ct);

    private Task<bool> NombrePatrullaUsadoAsync(long unidadId, string nombre, long? excluirId, CancellationToken ct)
    {
        var normalizado = nombre.Trim().ToLower();
        return db.Patrullas.AnyAsync(p => p.UnidadId == unidadId && p.Id != excluirId && p.Nombre.ToLower() == normalizado, ct);
    }

    private BadRequestObjectResult? ValidarRama(string rama) =>
        Ramas.Todas.Contains(rama) ? null : BadRequest(ResponseData.Error("La rama no es válida.", [$"Ramas válidas: {string.Join(", ", Ramas.Todas)}."]));

    private NotFoundObjectResult UnidadNoEncontrada() => NotFound(ResponseData.Error("No se encontró la unidad."));

    private NotFoundObjectResult PatrullaNoEncontrada() => NotFound(ResponseData.Error("No se encontró la patrulla."));
}

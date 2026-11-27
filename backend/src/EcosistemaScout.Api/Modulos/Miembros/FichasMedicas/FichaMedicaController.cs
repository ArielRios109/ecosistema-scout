using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;

/// <summary>Ficha médica descifrada. Si el beneficiario aún no tiene ficha, Existe es false y los campos vienen vacíos.</summary>
public record FichaMedicaDto(
    bool Existe,
    string? Prevision,
    string? GrupoSanguineo,
    string? Alergias,
    string? Enfermedades,
    string? Medicamentos,
    string? RestriccionesAlimentarias,
    string? NecesidadesEmocionales,
    DateTime? ActualizadaEl,
    string? ActualizadaPor);

public record SolicitudFichaMedica(
    [MaxLength(40, ErrorMessage = "La previsión admite hasta 40 caracteres.")] string? Prevision,
    [RegularExpression("^(?i)(A|B|AB|O)[+-]$", ErrorMessage = "El grupo sanguíneo no es válido (ej.: O+, AB-).")] string? GrupoSanguineo,
    [MaxLength(1000, ErrorMessage = "Las alergias admiten hasta 1000 caracteres.")] string? Alergias,
    [MaxLength(1000, ErrorMessage = "Las enfermedades admiten hasta 1000 caracteres.")] string? Enfermedades,
    [MaxLength(1000, ErrorMessage = "Los medicamentos admiten hasta 1000 caracteres.")] string? Medicamentos,
    [MaxLength(1000, ErrorMessage = "Las restricciones alimentarias admiten hasta 1000 caracteres.")] string? RestriccionesAlimentarias,
    [MaxLength(1000, ErrorMessage = "Las necesidades emocionales admiten hasta 1000 caracteres.")] string? NecesidadesEmocionales);

/// <summary>
/// Ficha médica y de necesidades emocionales del beneficiario. Solo la ven y editan administración y dirigentes
/// (política Miembros). Los datos se guardan cifrados y se registra quién la modificó por última vez.
/// </summary>
[ApiController]
[Route("api/miembros/beneficiarios/{beneficiarioId:long}/ficha-medica")]
[Authorize(Policy = Politicas.Miembros)]
public class FichaMedicaController(AppDbContext db, CifradoFichaMedica cifrado, IUsuarioActual usuarioActual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(long beneficiarioId, CancellationToken ct)
    {
        if (!await db.Beneficiarios.AnyAsync(b => b.Id == beneficiarioId, ct)) return BeneficiarioNoEncontrado();
        return Ok(ResponseData.Ok(await DtoAsync(beneficiarioId, ct)));
    }

    /// <summary>Crea o actualiza la ficha médica.</summary>
    [HttpPut]
    public async Task<IActionResult> Guardar(long beneficiarioId, SolicitudFichaMedica solicitud, CancellationToken ct)
    {
        if (!await db.Beneficiarios.AnyAsync(b => b.Id == beneficiarioId, ct)) return BeneficiarioNoEncontrado();

        var ficha = await db.FichasMedicas.FirstOrDefaultAsync(f => f.BeneficiarioId == beneficiarioId, ct);
        if (ficha is null)
        {
            ficha = new FichaMedica { BeneficiarioId = beneficiarioId };
            db.FichasMedicas.Add(ficha);
        }

        ficha.Prevision = cifrado.Cifrar(solicitud.Prevision);
        ficha.GrupoSanguineo = cifrado.Cifrar(solicitud.GrupoSanguineo?.ToUpperInvariant());
        ficha.Alergias = cifrado.Cifrar(solicitud.Alergias);
        ficha.Enfermedades = cifrado.Cifrar(solicitud.Enfermedades);
        ficha.Medicamentos = cifrado.Cifrar(solicitud.Medicamentos);
        ficha.RestriccionesAlimentarias = cifrado.Cifrar(solicitud.RestriccionesAlimentarias);
        ficha.NecesidadesEmocionales = cifrado.Cifrar(solicitud.NecesidadesEmocionales);
        ficha.ActualizadoPor = usuarioActual.Id;
        await db.SaveChangesAsync(ct);

        return Ok(ResponseData.Ok(await DtoAsync(beneficiarioId, ct), "Ficha médica guardada."));
    }

    private async Task<FichaMedicaDto> DtoAsync(long beneficiarioId, CancellationToken ct)
    {
        var ficha = await db.FichasMedicas.AsNoTracking().FirstOrDefaultAsync(f => f.BeneficiarioId == beneficiarioId, ct);
        if (ficha is null) return new FichaMedicaDto(false, null, null, null, null, null, null, null, null, null);

        var autor = ficha.ActualizadoPor is { } id
            ? await db.Users.Where(u => u.Id == id).Select(u => u.Nombre).FirstOrDefaultAsync(ct)
            : null;
        return new FichaMedicaDto(true,
            cifrado.Descifrar(ficha.Prevision),
            cifrado.Descifrar(ficha.GrupoSanguineo),
            cifrado.Descifrar(ficha.Alergias),
            cifrado.Descifrar(ficha.Enfermedades),
            cifrado.Descifrar(ficha.Medicamentos),
            cifrado.Descifrar(ficha.RestriccionesAlimentarias),
            cifrado.Descifrar(ficha.NecesidadesEmocionales),
            ficha.UpdatedAt,
            autor);
    }

    private NotFoundObjectResult BeneficiarioNoEncontrado() => NotFound(ResponseData.Error("No se encontró el beneficiario."));
}

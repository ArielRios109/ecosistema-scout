using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Miembros.Contactos;

public record ContactoDto(long Id, string Nombre, string? Parentesco, string Telefono);

public record SolicitudContacto(
    [Required(ErrorMessage = "Ingresa el nombre del contacto.")][MaxLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")] string Nombre,
    [MaxLength(40, ErrorMessage = "El parentesco admite hasta 40 caracteres.")] string? Parentesco,
    [Required(ErrorMessage = "Ingresa el teléfono.")]
    [RegularExpression(@"^\+?[0-9 ()-]{8,20}$", ErrorMessage = "El teléfono no es válido.")]
    string Telefono);

/// <summary>Contactos de emergencia de cada beneficiario (a quién llamar si pasa algo en una actividad).</summary>
[ApiController]
[Route("api/miembros/beneficiarios/{beneficiarioId:long}/contactos")]
[Authorize(Policy = Politicas.Miembros)]
public class ContactosController(AppDbContext db) : ControllerBase
{
    public const int Maximo = 5;

    [HttpGet]
    public async Task<IActionResult> Listar(long beneficiarioId, CancellationToken ct)
    {
        if (!await db.Beneficiarios.AnyAsync(b => b.Id == beneficiarioId, ct)) return BeneficiarioNoEncontrado();
        return Ok(ResponseData.Ok(await ListaAsync(beneficiarioId, ct)));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(long beneficiarioId, SolicitudContacto solicitud, CancellationToken ct)
    {
        if (!await db.Beneficiarios.AnyAsync(b => b.Id == beneficiarioId, ct)) return BeneficiarioNoEncontrado();
        if (await db.ContactosEmergencia.CountAsync(c => c.BeneficiarioId == beneficiarioId, ct) >= Maximo)
        {
            return Conflict(ResponseData.Error($"Se admiten hasta {Maximo} contactos de emergencia por beneficiario."));
        }

        var contacto = new ContactoEmergencia { BeneficiarioId = beneficiarioId };
        Copiar(solicitud, contacto);
        db.ContactosEmergencia.Add(contacto);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await ListaAsync(beneficiarioId, ct), "Contacto de emergencia agregado."));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Editar(long beneficiarioId, long id, SolicitudContacto solicitud, CancellationToken ct)
    {
        var contacto = await db.ContactosEmergencia.FirstOrDefaultAsync(c => c.Id == id && c.BeneficiarioId == beneficiarioId, ct);
        if (contacto is null) return NoEncontrado();

        Copiar(solicitud, contacto);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await ListaAsync(beneficiarioId, ct), "Contacto de emergencia actualizado."));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long beneficiarioId, long id, CancellationToken ct)
    {
        var contacto = await db.ContactosEmergencia.FirstOrDefaultAsync(c => c.Id == id && c.BeneficiarioId == beneficiarioId, ct);
        if (contacto is null) return NoEncontrado();

        db.ContactosEmergencia.Remove(contacto);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await ListaAsync(beneficiarioId, ct), "Contacto de emergencia eliminado."));
    }

    private Task<List<ContactoDto>> ListaAsync(long beneficiarioId, CancellationToken ct) =>
        db.ContactosEmergencia.AsNoTracking()
            .Where(c => c.BeneficiarioId == beneficiarioId)
            .OrderBy(c => c.Id)
            .Select(c => new ContactoDto(c.Id, c.Nombre, c.Parentesco, c.Telefono))
            .ToListAsync(ct);

    private static void Copiar(SolicitudContacto solicitud, ContactoEmergencia contacto)
    {
        contacto.Nombre = solicitud.Nombre.Trim();
        contacto.Parentesco = string.IsNullOrWhiteSpace(solicitud.Parentesco) ? null : solicitud.Parentesco.Trim();
        contacto.Telefono = solicitud.Telefono.Trim();
    }

    private NotFoundObjectResult NoEncontrado() => NotFound(ResponseData.Error("No se encontró el contacto de emergencia."));
    private NotFoundObjectResult BeneficiarioNoEncontrado() => NotFound(ResponseData.Error("No se encontró el beneficiario."));
}

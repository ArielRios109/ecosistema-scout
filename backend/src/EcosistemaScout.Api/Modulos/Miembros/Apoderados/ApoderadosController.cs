using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using EcosistemaScout.Api.Modulos.Base.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Miembros.Apoderados;

public record ApoderadoResumen(
    long Id, string Rut, string Nombres, string Apellidos, string Email, string? Telefono, bool TieneCuenta, int Beneficiarios);

public record BeneficiarioVinculado(long BeneficiarioId, string Nombre, string Unidad, string Estado, string Parentesco, bool EsPrincipal);

public record ApoderadoDetalle(
    long Id, string Rut, string Nombres, string Apellidos, string Email, string? Telefono, bool TieneCuenta,
    IReadOnlyList<BeneficiarioVinculado> Beneficiarios);

public record SolicitudApoderado(
    [Required(ErrorMessage = "Ingresa el RUT.")][RutValido] string Rut,
    [Required(ErrorMessage = "Ingresa los nombres.")][MaxLength(80, ErrorMessage = "Los nombres admiten hasta 80 caracteres.")] string Nombres,
    [Required(ErrorMessage = "Ingresa los apellidos.")][MaxLength(80, ErrorMessage = "Los apellidos admiten hasta 80 caracteres.")] string Apellidos,
    [Required(ErrorMessage = "Ingresa el correo.")][EmailAddress(ErrorMessage = "El correo no es válido.")][MaxLength(120, ErrorMessage = "El correo admite hasta 120 caracteres.")] string Email,
    [MaxLength(20, ErrorMessage = "El teléfono admite hasta 20 caracteres.")] string? Telefono);

/// <summary>Resultado de crear la cuenta del Portal: la contraseña temporal solo viene si la cuenta es nueva.</summary>
public record CuentaPortal(UsuarioResumen Usuario, string? PasswordTemporal);

public record VinculoApoderado(
    long ApoderadoId, string Rut, string Nombre, string Email, string? Telefono, bool TieneCuenta, string Parentesco, bool EsPrincipal);

public record SolicitudVinculo(
    long ApoderadoId,
    [Required(ErrorMessage = "Indica el parentesco.")][MaxLength(40, ErrorMessage = "El parentesco admite hasta 40 caracteres.")] string Parentesco,
    bool EsPrincipal);

public record SolicitudEditarVinculo(
    [Required(ErrorMessage = "Indica el parentesco.")][MaxLength(40, ErrorMessage = "El parentesco admite hasta 40 caracteres.")] string Parentesco,
    bool EsPrincipal);

/// <summary>
/// Apoderados y su vínculo con los beneficiarios (parentesco y apoderado principal).
/// Cada beneficiario tiene a lo más un apoderado principal. El administrador puede crearles su cuenta del Portal.
/// </summary>
[ApiController]
[Route("api/miembros")]
[Authorize(Policy = Politicas.Miembros)]
public class ApoderadosController(AppDbContext db, UserManager<Usuario> usuarios) : ControllerBase
{
    [HttpGet("apoderados")]
    public async Task<ResponseData<Pagina<ApoderadoResumen>>> Listar([FromQuery] ConsultaPaginada filtro, CancellationToken ct)
    {
        var consulta = db.Apoderados.AsNoTracking();
        if (filtro.BuscarNormalizado is { } texto)
        {
            var rut = texto.Replace(".", "").Replace(" ", "");
            consulta = consulta.Where(a =>
                (a.Nombres + " " + a.Apellidos).ToLower().Contains(texto)
                || a.Email.ToLower().Contains(texto)
                || a.Rut.ToLower().Contains(rut));
        }

        var pagina = await consulta
            .OrderBy(a => a.Apellidos).ThenBy(a => a.Nombres)
            .Select(a => new ApoderadoResumen(a.Id, a.Rut, a.Nombres, a.Apellidos, a.Email, a.Telefono,
                a.UsuarioId != null, a.Beneficiarios.Count))
            .PaginarAsync(filtro, ct);

        return ResponseData.Ok(pagina with { Elementos = pagina.Elementos.Select(a => a with { Rut = Rut.Formatear(a.Rut) }).ToList() });
    }

    /// <summary>Indica si ya hay un apoderado con ese RUT, para avisarlo en el formulario antes de pedir el resto de los datos.</summary>
    [HttpGet("apoderados/existe")]
    public async Task<IActionResult> Existe([FromQuery] string? rut, CancellationToken ct)
    {
        if (!Rut.EsValido(rut)) return BadRequest(ResponseData.Error("El RUT no es válido."));
        var normalizado = Rut.Normalizar(rut!);
        var registrado = await db.Apoderados.AsNoTracking()
            .Where(x => x.Rut == normalizado)
            .Select(x => new RutRegistrado(true, x.Id, x.Nombres + " " + x.Apellidos))
            .FirstOrDefaultAsync(ct);
        return Ok(ResponseData.Ok(registrado ?? RutRegistrado.Libre));
    }

    [HttpGet("apoderados/{id:long}")]
    public async Task<IActionResult> Obtener(long id, CancellationToken ct)
    {
        var detalle = await DetalleAsync(id, ct);
        return detalle is null ? NoEncontrado() : Ok(ResponseData.Ok(detalle));
    }

    [HttpPost("apoderados")]
    public async Task<IActionResult> Crear(SolicitudApoderado solicitud, CancellationToken ct)
    {
        var rut = Rut.Normalizar(solicitud.Rut);
        if (await db.Apoderados.AnyAsync(a => a.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe un apoderado con ese RUT."));
        }

        var apoderado = new Apoderado();
        Copiar(solicitud, apoderado, rut);
        db.Apoderados.Add(apoderado);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = apoderado.Id },
            ResponseData.Ok(await DetalleAsync(apoderado.Id, ct), "Apoderado registrado."));
    }

    [HttpPut("apoderados/{id:long}")]
    public async Task<IActionResult> Editar(long id, SolicitudApoderado solicitud, CancellationToken ct)
    {
        var apoderado = await db.Apoderados.FindAsync([id], ct);
        if (apoderado is null) return NoEncontrado();

        var rut = Rut.Normalizar(solicitud.Rut);
        if (await db.Apoderados.AnyAsync(a => a.Id != id && a.Rut == rut, ct))
        {
            return Conflict(ResponseData.Error("Ya existe otro apoderado con ese RUT."));
        }

        Copiar(solicitud, apoderado, rut);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Datos del apoderado actualizados."));
    }

    /// <summary>Solo se elimina un apoderado sin beneficiarios vinculados.</summary>
    [HttpDelete("apoderados/{id:long}")]
    public async Task<IActionResult> Eliminar(long id, CancellationToken ct)
    {
        var apoderado = await db.Apoderados.FindAsync([id], ct);
        if (apoderado is null) return NoEncontrado();
        if (await db.BeneficiarioApoderado.AnyAsync(v => v.ApoderadoId == id, ct))
        {
            return Conflict(ResponseData.Error("El apoderado tiene beneficiarios vinculados. Quita los vínculos antes de eliminarlo."));
        }

        db.Apoderados.Remove(apoderado);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok<object?>(null, "Apoderado eliminado."));
    }

    /// <summary>
    /// Crea la cuenta del Portal de pagos con rol apoderado y una contraseña temporal.
    /// Si ya existe un usuario con ese correo, se vincula y se le agrega el rol (sin cambiar su contraseña).
    /// </summary>
    [HttpPost("apoderados/{id:long}/cuenta")]
    [Authorize(Policy = Politicas.Administracion)]
    public async Task<IActionResult> CrearCuenta(long id, CancellationToken ct)
    {
        var apoderado = await db.Apoderados.FindAsync([id], ct);
        if (apoderado is null) return NoEncontrado();
        if (apoderado.UsuarioId is not null)
        {
            return Conflict(ResponseData.Error("El apoderado ya tiene una cuenta del Portal."));
        }

        string? password = null;
        var usuario = await usuarios.FindByEmailAsync(apoderado.Email);
        if (usuario is not null)
        {
            if (await db.Apoderados.AnyAsync(a => a.UsuarioId == usuario.Id, ct))
            {
                return Conflict(ResponseData.Error("Ese correo ya pertenece a la cuenta de otro apoderado."));
            }
            if (!await usuarios.IsInRoleAsync(usuario, Roles.Apoderado))
            {
                await usuarios.AddToRoleAsync(usuario, Roles.Apoderado);
            }
        }
        else
        {
            password = GeneradorPassword.Generar();
            usuario = new Usuario
            {
                Nombre = $"{apoderado.Nombres} {apoderado.Apellidos}",
                Email = apoderado.Email,
                UserName = apoderado.Email,
                EmailConfirmed = true,
                DebeCambiarPassword = true,
            };
            var resultado = await usuarios.CreateAsync(usuario, password);
            if (!resultado.Succeeded)
            {
                return BadRequest(ResponseData.Error("No se pudo crear la cuenta.", resultado.Errors.Select(e => e.Description).ToList()));
            }
            await usuarios.AddToRoleAsync(usuario, Roles.Apoderado);
        }

        apoderado.UsuarioId = usuario.Id;
        await db.SaveChangesAsync(ct);

        var roles = (await usuarios.GetRolesAsync(usuario)).ToList();
        var resumen = new UsuarioResumen(usuario.Id, usuario.Nombre, usuario.Email!, roles, usuario.Activo,
            usuario.DebeCambiarPassword, usuario.UltimoAcceso);
        var mensaje = password is null
            ? "Se vinculó la cuenta existente con ese correo y se le dio acceso al Portal."
            : $"Cuenta del Portal creada. Comparte la contraseña temporal con {usuario.Nombre}.";
        return Ok(ResponseData.Ok(new CuentaPortal(resumen, password), mensaje));
    }

    // ----- Vínculos con beneficiarios -----

    [HttpGet("beneficiarios/{beneficiarioId:long}/apoderados")]
    public async Task<IActionResult> Vinculos(long beneficiarioId, CancellationToken ct)
    {
        if (!await db.Beneficiarios.AnyAsync(b => b.Id == beneficiarioId, ct)) return BeneficiarioNoEncontrado();
        return Ok(ResponseData.Ok(await VinculosAsync(beneficiarioId, ct)));
    }

    [HttpPost("beneficiarios/{beneficiarioId:long}/apoderados")]
    public async Task<IActionResult> Vincular(long beneficiarioId, SolicitudVinculo solicitud, CancellationToken ct)
    {
        if (!await db.Beneficiarios.AnyAsync(b => b.Id == beneficiarioId, ct)) return BeneficiarioNoEncontrado();
        if (!await db.Apoderados.AnyAsync(a => a.Id == solicitud.ApoderadoId, ct)) return NoEncontrado();
        if (await db.BeneficiarioApoderado.AnyAsync(v => v.BeneficiarioId == beneficiarioId && v.ApoderadoId == solicitud.ApoderadoId, ct))
        {
            return Conflict(ResponseData.Error("El apoderado ya está vinculado a este beneficiario."));
        }

        // El primer apoderado queda como principal aunque no se indique.
        var esPrincipal = solicitud.EsPrincipal || !await db.BeneficiarioApoderado.AnyAsync(v => v.BeneficiarioId == beneficiarioId, ct);
        if (esPrincipal) await QuitarPrincipalAsync(beneficiarioId, ct);

        db.BeneficiarioApoderado.Add(new BeneficiarioApoderado
        {
            BeneficiarioId = beneficiarioId,
            ApoderadoId = solicitud.ApoderadoId,
            Parentesco = solicitud.Parentesco.Trim(),
            EsPrincipal = esPrincipal,
        });
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await VinculosAsync(beneficiarioId, ct), "Apoderado vinculado."));
    }

    [HttpPut("beneficiarios/{beneficiarioId:long}/apoderados/{apoderadoId:long}")]
    public async Task<IActionResult> EditarVinculo(long beneficiarioId, long apoderadoId, SolicitudEditarVinculo solicitud, CancellationToken ct)
    {
        var vinculo = await db.BeneficiarioApoderado.FindAsync([beneficiarioId, apoderadoId], ct);
        if (vinculo is null) return VinculoNoEncontrado();

        if (solicitud.EsPrincipal && !vinculo.EsPrincipal) await QuitarPrincipalAsync(beneficiarioId, ct);
        vinculo.Parentesco = solicitud.Parentesco.Trim();
        vinculo.EsPrincipal = solicitud.EsPrincipal;
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await VinculosAsync(beneficiarioId, ct), "Vínculo actualizado."));
    }

    [HttpDelete("beneficiarios/{beneficiarioId:long}/apoderados/{apoderadoId:long}")]
    public async Task<IActionResult> Desvincular(long beneficiarioId, long apoderadoId, CancellationToken ct)
    {
        var vinculo = await db.BeneficiarioApoderado.FindAsync([beneficiarioId, apoderadoId], ct);
        if (vinculo is null) return VinculoNoEncontrado();

        db.BeneficiarioApoderado.Remove(vinculo);
        // Si se quita al principal, el siguiente apoderado pasa a ser principal.
        if (vinculo.EsPrincipal)
        {
            var siguiente = await db.BeneficiarioApoderado
                .Where(v => v.BeneficiarioId == beneficiarioId && v.ApoderadoId != apoderadoId)
                .OrderBy(v => v.ApoderadoId).FirstOrDefaultAsync(ct);
            if (siguiente is not null) siguiente.EsPrincipal = true;
        }
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await VinculosAsync(beneficiarioId, ct), "Vínculo eliminado."));
    }

    private async Task QuitarPrincipalAsync(long beneficiarioId, CancellationToken ct)
    {
        var principales = await db.BeneficiarioApoderado.Where(v => v.BeneficiarioId == beneficiarioId && v.EsPrincipal).ToListAsync(ct);
        foreach (var v in principales) v.EsPrincipal = false;
    }

    private async Task<IReadOnlyList<VinculoApoderado>> VinculosAsync(long beneficiarioId, CancellationToken ct)
    {
        var vinculos = await db.BeneficiarioApoderado.AsNoTracking()
            .Where(v => v.BeneficiarioId == beneficiarioId)
            .OrderByDescending(v => v.EsPrincipal).ThenBy(v => v.Apoderado.Apellidos)
            .Select(v => new VinculoApoderado(v.ApoderadoId, v.Apoderado.Rut, v.Apoderado.Nombres + " " + v.Apoderado.Apellidos,
                v.Apoderado.Email, v.Apoderado.Telefono, v.Apoderado.UsuarioId != null, v.Parentesco, v.EsPrincipal))
            .ToListAsync(ct);
        return vinculos.Select(v => v with { Rut = Rut.Formatear(v.Rut) }).ToList();
    }

    private async Task<ApoderadoDetalle?> DetalleAsync(long id, CancellationToken ct)
    {
        var a = await db.Apoderados.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return null;

        var beneficiarios = await db.BeneficiarioApoderado.AsNoTracking()
            .Where(v => v.ApoderadoId == id)
            .OrderBy(v => v.Beneficiario.Apellidos)
            .Select(v => new BeneficiarioVinculado(v.BeneficiarioId, v.Beneficiario.Nombres + " " + v.Beneficiario.Apellidos,
                v.Beneficiario.Unidad.Nombre, v.Beneficiario.Estado, v.Parentesco, v.EsPrincipal))
            .ToListAsync(ct);
        return new ApoderadoDetalle(a.Id, Rut.Formatear(a.Rut), a.Nombres, a.Apellidos, a.Email, a.Telefono, a.UsuarioId != null, beneficiarios);
    }

    /// <summary>Copia los datos de la solicitud (también lo usa la inscripción de beneficiarios).</summary>
    internal static void Copiar(SolicitudApoderado solicitud, Apoderado apoderado, string rut)
    {
        apoderado.Rut = rut;
        apoderado.Nombres = solicitud.Nombres.Trim();
        apoderado.Apellidos = solicitud.Apellidos.Trim();
        apoderado.Email = solicitud.Email.Trim().ToLowerInvariant();
        apoderado.Telefono = string.IsNullOrWhiteSpace(solicitud.Telefono) ? null : solicitud.Telefono.Trim();
    }

    private NotFoundObjectResult NoEncontrado() => NotFound(ResponseData.Error("No se encontró el apoderado."));
    private NotFoundObjectResult BeneficiarioNoEncontrado() => NotFound(ResponseData.Error("No se encontró el beneficiario."));
    private NotFoundObjectResult VinculoNoEncontrado() => NotFound(ResponseData.Error("El apoderado no está vinculado a este beneficiario."));
}

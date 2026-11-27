using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Base.Usuarios;

public record UsuarioResumen(
    long Id, string Nombre, string Email, IReadOnlyList<string> Roles, bool Activo, bool DebeCambiarPassword, DateTime? UltimoAcceso);

public record SolicitudCrearUsuario(
    [Required(ErrorMessage = "Ingresa el nombre.")][MaxLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")] string Nombre,
    [Required(ErrorMessage = "Ingresa el correo.")][EmailAddress(ErrorMessage = "El correo no es válido.")][MaxLength(120, ErrorMessage = "El correo admite hasta 120 caracteres.")] string Email,
    [Required(ErrorMessage = "Asigna al menos un rol.")][MinLength(1, ErrorMessage = "Asigna al menos un rol.")] IReadOnlyList<string> Roles);

public record SolicitudEditarUsuario(
    [Required(ErrorMessage = "Ingresa el nombre.")][MaxLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")] string Nombre,
    [Required(ErrorMessage = "Ingresa el correo.")][EmailAddress(ErrorMessage = "El correo no es válido.")][MaxLength(120, ErrorMessage = "El correo admite hasta 120 caracteres.")] string Email);

public record SolicitudRoles(
    [Required(ErrorMessage = "Asigna al menos un rol.")][MinLength(1, ErrorMessage = "Asigna al menos un rol.")] IReadOnlyList<string> Roles);

/// <summary>Se entrega una sola vez al administrador para que se la comunique a la persona.</summary>
public record UsuarioConPasswordTemporal(UsuarioResumen Usuario, string PasswordTemporal);

public record RolInfo(string Nombre, string? Descripcion);

public class FiltroUsuarios : ConsultaPaginada
{
    public string? Rol { get; set; }
    public bool? Activo { get; set; }
}

/// <summary>Administración de cuentas de acceso (solo el rol administrador).</summary>
[ApiController]
[Route("api/base")]
[Authorize(Policy = Politicas.Administracion)]
public class UsuariosController(UserManager<Usuario> usuarios, RoleManager<Rol> roles) : ControllerBase
{
    /// <summary>Lista paginada de usuarios, con búsqueda por nombre o correo y filtros por rol y estado.</summary>
    [HttpGet("usuarios")]
    public async Task<ResponseData<Pagina<UsuarioResumen>>> Listar([FromQuery] FiltroUsuarios filtro, CancellationToken ct)
    {
        var consulta = usuarios.Users.AsNoTracking().Include(u => u.Roles).ThenInclude(ur => ur.Rol).AsQueryable();

        if (filtro.BuscarNormalizado is { } texto)
        {
            consulta = consulta.Where(u => u.Nombre.ToLower().Contains(texto) || u.Email!.ToLower().Contains(texto));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Rol))
        {
            consulta = consulta.Where(u => u.Roles.Any(r => r.Rol.Name == filtro.Rol));
        }
        if (filtro.Activo is { } activo)
        {
            consulta = consulta.Where(u => u.Activo == activo);
        }

        var pagina = await consulta.OrderBy(u => u.Nombre).PaginarAsync(filtro, ct);
        return ResponseData.Ok(new Pagina<UsuarioResumen>(
            pagina.Elementos.Select(Resumen).ToList(), pagina.Total, pagina.PaginaActual, pagina.Tamano));
    }

    [HttpGet("usuarios/{id:long}")]
    public async Task<IActionResult> Obtener(long id)
    {
        var usuario = await BuscarAsync(id);
        return usuario is null ? NoEncontrado() : Ok(ResponseData.Ok(Resumen(usuario)));
    }

    /// <summary>Crea una cuenta con una contraseña temporal que la persona debe cambiar al primer ingreso.</summary>
    [HttpPost("usuarios")]
    public async Task<IActionResult> Crear(SolicitudCrearUsuario solicitud)
    {
        if (await RolesInvalidosAsync(solicitud.Roles) is { } error) return error;
        if (await usuarios.FindByEmailAsync(solicitud.Email) is not null)
        {
            return Conflict(ResponseData.Error("Ya existe un usuario con ese correo."));
        }

        var password = GeneradorPassword.Generar();
        var usuario = new Usuario
        {
            Nombre = solicitud.Nombre.Trim(),
            Email = solicitud.Email.Trim(),
            UserName = solicitud.Email.Trim(),
            EmailConfirmed = true,
            DebeCambiarPassword = true,
        };
        var resultado = await usuarios.CreateAsync(usuario, password);
        if (!resultado.Succeeded) return ErrorIdentity(resultado);

        await usuarios.AddToRolesAsync(usuario, solicitud.Roles);
        var creado = (await BuscarAsync(usuario.Id))!;
        return CreatedAtAction(nameof(Obtener), new { id = usuario.Id }, ResponseData.Ok(
            new UsuarioConPasswordTemporal(Resumen(creado), password),
            $"Usuario creado. Comparte la contraseña temporal con {usuario.Nombre}."));
    }

    [HttpPut("usuarios/{id:long}")]
    public async Task<IActionResult> Editar(long id, SolicitudEditarUsuario solicitud)
    {
        var usuario = await usuarios.FindByIdAsync(id.ToString());
        if (usuario is null) return NoEncontrado();

        var otro = await usuarios.FindByEmailAsync(solicitud.Email);
        if (otro is not null && otro.Id != id)
        {
            return Conflict(ResponseData.Error("Ya existe otro usuario con ese correo."));
        }

        usuario.Nombre = solicitud.Nombre.Trim();
        usuario.Email = solicitud.Email.Trim();
        usuario.UserName = solicitud.Email.Trim();
        var resultado = await usuarios.UpdateAsync(usuario);
        if (!resultado.Succeeded) return ErrorIdentity(resultado);

        return Ok(ResponseData.Ok(Resumen((await BuscarAsync(id))!), "Usuario actualizado."));
    }

    /// <summary>Reemplaza los roles del usuario (los roles son acumulables).</summary>
    [HttpPut("usuarios/{id:long}/roles")]
    public async Task<IActionResult> AsignarRoles(long id, SolicitudRoles solicitud)
    {
        var usuario = await usuarios.FindByIdAsync(id.ToString());
        if (usuario is null) return NoEncontrado();
        if (await RolesInvalidosAsync(solicitud.Roles) is { } error) return error;

        if (id == IdActual() && !solicitud.Roles.Contains(Base.Roles.Administrador))
        {
            return BadRequest(ResponseData.Error("No puedes quitarte el rol de administrador a ti mismo."));
        }

        var actuales = await usuarios.GetRolesAsync(usuario);
        await usuarios.RemoveFromRolesAsync(usuario, actuales.Except(solicitud.Roles));
        await usuarios.AddToRolesAsync(usuario, solicitud.Roles.Except(actuales));
        return Ok(ResponseData.Ok(Resumen((await BuscarAsync(id))!), "Roles actualizados."));
    }

    [HttpPost("usuarios/{id:long}/activar")]
    public Task<IActionResult> Activar(long id) => CambiarEstado(id, true);

    [HttpPost("usuarios/{id:long}/desactivar")]
    public Task<IActionResult> Desactivar(long id) => CambiarEstado(id, false);

    /// <summary>Genera una nueva contraseña temporal; la persona deberá cambiarla al ingresar.</summary>
    [HttpPost("usuarios/{id:long}/restablecer-password")]
    public async Task<IActionResult> RestablecerPassword(long id)
    {
        var usuario = await usuarios.FindByIdAsync(id.ToString());
        if (usuario is null) return NoEncontrado();

        var password = GeneradorPassword.Generar();
        var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await usuarios.ResetPasswordAsync(usuario, token, password);
        if (!resultado.Succeeded) return ErrorIdentity(resultado);

        usuario.DebeCambiarPassword = true;
        await usuarios.UpdateAsync(usuario);
        await usuarios.SetLockoutEndDateAsync(usuario, null);
        await usuarios.ResetAccessFailedCountAsync(usuario);

        return Ok(ResponseData.Ok(
            new UsuarioConPasswordTemporal(Resumen((await BuscarAsync(id))!), password),
            "Contraseña restablecida. Comparte la contraseña temporal con la persona."));
    }

    /// <summary>Roles disponibles para asignar.</summary>
    [HttpGet("roles")]
    public async Task<ResponseData<IReadOnlyList<RolInfo>>> ListarRoles() =>
        ResponseData.Ok<IReadOnlyList<RolInfo>>(await roles.Roles.AsNoTracking()
            .OrderBy(r => r.Id).Select(r => new RolInfo(r.Name!, r.Descripcion)).ToListAsync());

    private async Task<IActionResult> CambiarEstado(long id, bool activo)
    {
        var usuario = await usuarios.FindByIdAsync(id.ToString());
        if (usuario is null) return NoEncontrado();
        if (!activo && id == IdActual())
        {
            return BadRequest(ResponseData.Error("No puedes desactivar tu propia cuenta."));
        }

        usuario.Activo = activo;
        await usuarios.UpdateAsync(usuario);
        // Al desactivar se invalida el sello de seguridad para que futuras validaciones lo detecten.
        if (!activo) await usuarios.UpdateSecurityStampAsync(usuario);

        return Ok(ResponseData.Ok(Resumen((await BuscarAsync(id))!), activo ? "Usuario activado." : "Usuario desactivado."));
    }

    private async Task<IActionResult?> RolesInvalidosAsync(IEnumerable<string> nombres)
    {
        var existentes = await roles.Roles.Select(r => r.Name!).ToListAsync();
        var invalidos = nombres.Where(n => !existentes.Contains(n)).ToList();
        return invalidos.Count == 0
            ? null
            : BadRequest(ResponseData.Error("Hay roles que no existen.", invalidos));
    }

    private Task<Usuario?> BuscarAsync(long id) =>
        usuarios.Users.AsNoTracking().Include(u => u.Roles).ThenInclude(ur => ur.Rol).FirstOrDefaultAsync(u => u.Id == id);

    private long? IdActual() =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static UsuarioResumen Resumen(Usuario u) => new(
        u.Id, u.Nombre, u.Email!, u.Roles.Select(r => r.Rol.Name!).OrderBy(n => n).ToList(),
        u.Activo, u.DebeCambiarPassword, u.UltimoAcceso);

    private NotFoundObjectResult NoEncontrado() => NotFound(ResponseData.Error("No se encontró el usuario."));

    private BadRequestObjectResult ErrorIdentity(IdentityResult resultado) =>
        BadRequest(ResponseData.Error("No se pudo guardar el usuario.", resultado.Errors.Select(e => e.Description).ToList()));
}

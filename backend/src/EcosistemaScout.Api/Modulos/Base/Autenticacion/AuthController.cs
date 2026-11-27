using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EcosistemaScout.Api.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcosistemaScout.Api.Modulos.Base.Autenticacion;

public record SolicitudLogin(
    [Required(ErrorMessage = "Ingresa tu correo.")][EmailAddress(ErrorMessage = "El correo no es válido.")] string Email,
    [Required(ErrorMessage = "Ingresa tu contraseña.")] string Password);

public record UsuarioActual(long Id, string Nombre, string Email, IReadOnlyList<string> Roles);

public record RespuestaLogin(string Token, DateTime Expira, UsuarioActual Usuario);

/// <summary>Inicio de sesión con correo y contraseña; entrega un token JWT.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<Usuario> usuarios,
    SignInManager<Usuario> acceso,
    ServicioTokens tokens,
    TimeProvider reloj) : ControllerBase
{
    /// <summary>Valida las credenciales y devuelve un token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<ResponseData<RespuestaLogin>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ResponseData<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(SolicitudLogin solicitud)
    {
        var usuario = await usuarios.FindByEmailAsync(solicitud.Email);
        if (usuario is null || !usuario.Activo)
        {
            return CredencialesInvalidas();
        }

        var resultado = await acceso.CheckPasswordSignInAsync(usuario, solicitud.Password, lockoutOnFailure: true);
        if (resultado.IsLockedOut)
        {
            return Unauthorized(ResponseData.Error(
                "Cuenta bloqueada temporalmente por demasiados intentos fallidos. Intenta en 15 minutos."));
        }
        if (!resultado.Succeeded)
        {
            return CredencialesInvalidas();
        }

        usuario.UltimoAcceso = reloj.GetUtcNow().UtcDateTime;
        await usuarios.UpdateAsync(usuario);

        var roles = (await usuarios.GetRolesAsync(usuario)).ToList();
        var (token, expira) = tokens.Generar(usuario, roles);

        var datos = new RespuestaLogin(token, expira, new UsuarioActual(usuario.Id, usuario.Nombre, usuario.Email!, roles));
        return Ok(ResponseData.Ok(datos, $"Bienvenido, {usuario.Nombre}."));
    }

    /// <summary>Datos del usuario que hace la petición (requiere token).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<ResponseData<UsuarioActual>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ResponseData<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuario = id is null ? null : await usuarios.FindByIdAsync(id);
        if (usuario is null || !usuario.Activo)
        {
            return Unauthorized(ResponseData.Error("Tu sesión ya no es válida. Vuelve a iniciar sesión."));
        }

        var roles = (await usuarios.GetRolesAsync(usuario)).ToList();
        return Ok(ResponseData.Ok(new UsuarioActual(usuario.Id, usuario.Nombre, usuario.Email!, roles)));
    }

    private UnauthorizedObjectResult CredencialesInvalidas() =>
        Unauthorized(ResponseData.Error("Correo o contraseña incorrectos."));
}

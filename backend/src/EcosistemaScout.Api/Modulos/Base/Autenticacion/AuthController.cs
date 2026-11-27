using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EcosistemaScout.Api.Modulos.Base.Autenticacion;

public record SolicitudLogin([Required, EmailAddress] string Email, [Required] string Password);

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
    [ProducesResponseType<RespuestaLogin>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            return Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Cuenta bloqueada temporalmente por demasiados intentos fallidos.");
        }
        if (!resultado.Succeeded)
        {
            return CredencialesInvalidas();
        }

        usuario.UltimoAcceso = reloj.GetUtcNow().UtcDateTime;
        await usuarios.UpdateAsync(usuario);

        var roles = (await usuarios.GetRolesAsync(usuario)).ToList();
        var (token, expira) = tokens.Generar(usuario, roles);

        return Ok(new RespuestaLogin(token, expira,
            new UsuarioActual(usuario.Id, usuario.Nombre, usuario.Email!, roles)));
    }

    /// <summary>Datos del usuario que hace la petición (requiere token).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UsuarioActual>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuario = id is null ? null : await usuarios.FindByIdAsync(id);
        if (usuario is null || !usuario.Activo)
        {
            return Unauthorized();
        }

        var roles = (await usuarios.GetRolesAsync(usuario)).ToList();
        return Ok(new UsuarioActual(usuario.Id, usuario.Nombre, usuario.Email!, roles));
    }

    private ObjectResult CredencialesInvalidas() =>
        Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Correo o contraseña incorrectos.");
}

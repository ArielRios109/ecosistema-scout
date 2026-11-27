using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EcosistemaScout.Api.Modulos.Base.Autenticacion;

/// <summary>Genera los tokens JWT que el frontend envía en cada petición.</summary>
public class ServicioTokens(IOptions<JwtOpciones> opciones, TimeProvider reloj)
{
    private readonly JwtOpciones _opciones = opciones.Value;

    public (string Token, DateTime Expira) Generar(Usuario usuario, IEnumerable<string> roles)
    {
        var expira = reloj.GetUtcNow().UtcDateTime.AddMinutes(_opciones.ExpiracionMinutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email!),
            new(JwtRegisteredClaimNames.Name, usuario.Nombre),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        if (usuario.DebeCambiarPassword)
        {
            // Con contraseña temporal solo puede cambiarla (ver FiltroCambioPassword).
            claims.Add(new Claim(ClaimsTali.CambioPasswordPendiente, "true"));
        }

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Clave)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

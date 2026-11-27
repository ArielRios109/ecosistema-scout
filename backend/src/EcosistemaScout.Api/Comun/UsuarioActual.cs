using System.Security.Claims;

namespace EcosistemaScout.Api.Comun;

/// <summary>Datos de quien hace la petición (para la bitácora y para registrar quién modificó algo).</summary>
public interface IUsuarioActual
{
    long? Id { get; }
    string? Ip { get; }
}

public class ServicioUsuarioActual(IHttpContextAccessor accesor) : IUsuarioActual
{
    public long? Id
    {
        get
        {
            var valor = accesor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(valor, out var id) ? id : null;
        }
    }

    public string? Ip => accesor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

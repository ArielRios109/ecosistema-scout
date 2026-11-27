using EcosistemaScout.Api.Comun;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EcosistemaScout.Api.Modulos.Base.Autenticacion;

/// <summary>Nombres de claims propios de Talí.</summary>
public static class ClaimsTali
{
    /// <summary>Presente cuando el usuario ingresó con una contraseña temporal y aún no la cambia.</summary>
    public const string CambioPasswordPendiente = "cambio_password_pendiente";
}

/// <summary>Marca acciones permitidas aunque el usuario tenga pendiente el cambio de contraseña.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class PermitirConCambioPendienteAttribute : Attribute;

/// <summary>
/// Filtro global: un usuario con contraseña temporal no puede usar la API (403) hasta cambiarla,
/// salvo en las acciones marcadas con <see cref="PermitirConCambioPendienteAttribute"/>.
/// </summary>
public sealed class FiltroCambioPassword : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext contexto)
    {
        var usuario = contexto.HttpContext.User;
        if (usuario.Identity?.IsAuthenticated != true || !usuario.HasClaim(ClaimsTali.CambioPasswordPendiente, "true"))
        {
            return;
        }

        var permitido = contexto.ActionDescriptor.EndpointMetadata.OfType<PermitirConCambioPendienteAttribute>().Any();
        if (!permitido)
        {
            contexto.Result = new ObjectResult(ResponseData.Error("Debes cambiar tu contraseña temporal antes de continuar."))
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }
    }
}

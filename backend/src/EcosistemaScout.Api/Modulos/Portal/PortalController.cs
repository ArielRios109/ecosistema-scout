using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcosistemaScout.Api.Modulos.Portal;

/// <summary>Módulo Portal de pagos (provisorio): ya protegido por rol.</summary>
[ApiController]
[Route("api/portal")]
[Authorize(Policy = Politicas.Portal)]
public class PortalController : ControllerBase
{
    [HttpGet]
    public EstadoModulo Get() => EstadoModulo.EnConstruccion("portal");
}

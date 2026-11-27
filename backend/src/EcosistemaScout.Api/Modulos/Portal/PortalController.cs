using EcosistemaScout.Api.Comun;
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
    public ResponseData<EstadoModulo> Get() => ResponseData.Ok(EstadoModulo.EnConstruccion("portal"));
}

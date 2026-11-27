using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcosistemaScout.Api.Modulos.Finanzas;

/// <summary>Módulo Financiamiento (provisorio): ya protegido por rol.</summary>
[ApiController]
[Route("api/finanzas")]
[Authorize(Policy = Politicas.Finanzas)]
public class FinanzasController : ControllerBase
{
    [HttpGet]
    public ResponseData<EstadoModulo> Get() => ResponseData.Ok(EstadoModulo.EnConstruccion("finanzas"));
}

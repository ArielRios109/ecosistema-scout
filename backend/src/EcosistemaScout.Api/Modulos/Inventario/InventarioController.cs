using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcosistemaScout.Api.Modulos.Inventario;

/// <summary>Módulo Inventario (provisorio): ya protegido por rol.</summary>
[ApiController]
[Route("api/inventario")]
[Authorize(Policy = Politicas.Inventario)]
public class InventarioController : ControllerBase
{
    [HttpGet]
    public EstadoModulo Get() => EstadoModulo.EnConstruccion("inventario");
}

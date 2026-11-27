using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcosistemaScout.Api.Modulos.Miembros;

/// <summary>Módulo Gestión de Miembros (provisorio): ya protegido por rol.</summary>
[ApiController]
[Route("api/miembros")]
[Authorize(Policy = Politicas.Miembros)]
public class MiembrosController : ControllerBase
{
    [HttpGet]
    public EstadoModulo Get() => EstadoModulo.EnConstruccion("miembros");
}

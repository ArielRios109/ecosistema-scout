using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcosistemaScout.Api.Modulos.Base;

public record EstadoSalud(string Api, string BaseDatos, DateTime Fecha);

/// <summary>Indica si la API y la base de datos están funcionando.</summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class SaludController(AppDbContext db, TimeProvider reloj) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ResponseData<EstadoSalud>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ResponseData<EstadoSalud>>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        bool conectada;
        try
        {
            conectada = await db.Database.CanConnectAsync(ct);
        }
        catch
        {
            conectada = false;
        }

        var estado = new EstadoSalud("ok", conectada ? "ok" : "sin conexión", reloj.GetUtcNow().UtcDateTime);
        return conectada
            ? Ok(ResponseData.Ok(estado))
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                ResponseData.ErrorConDatos("La base de datos no está disponible.", estado));
    }
}

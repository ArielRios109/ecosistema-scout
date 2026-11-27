using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Modulos.Base;

namespace EcosistemaScout.Tests;

public class SaludTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task Health_SinToken_RespondeOkConBaseDeDatos()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var estado = await respuesta.Content.ReadFromJsonAsync<EstadoSalud>();
        Assert.Equal("ok", estado!.Api);
        Assert.Equal("ok", estado.BaseDatos);
    }
}

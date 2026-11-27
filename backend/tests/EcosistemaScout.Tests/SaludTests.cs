using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
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
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResponseData<EstadoSalud>>();
        Assert.True(cuerpo!.Exito);
        Assert.Equal("ok", cuerpo.Datos!.Api);
        Assert.Equal("ok", cuerpo.Datos.BaseDatos);
    }
}

using System.Net;
using System.Text;

namespace EcosistemaScout.Tests;

public class ManejoErroresTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    [Theory]
    [InlineData("{\"nombre\": \"Tropa\", \"rama\": ")]
    [InlineData("{\"nombre\": 123, \"rama\": [\"scouts\"]}")]
    public async Task JsonInvalido_Devuelve400EnEspanol_SinDetallesInternos(string cuerpo)
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");

        var respuesta = await admin.PostAsync("/api/miembros/unidades", new StringContent(cuerpo, Encoding.UTF8, "application/json"));
        var texto = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Los datos enviados no tienen un formato válido.", texto);
        Assert.DoesNotContain("EcosistemaScout", texto);
        Assert.DoesNotContain("JSON value", texto);
    }
}

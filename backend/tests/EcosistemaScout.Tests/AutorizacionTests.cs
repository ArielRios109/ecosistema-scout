using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Base.Autenticacion;

namespace EcosistemaScout.Tests;

/// <summary>Verifica que cada rol solo acceda a los módulos que le corresponden (200) y no a los demás (403).</summary>
public class AutorizacionTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    [Theory]
    // Administrador: todos los módulos
    [InlineData("admin@demo.cl", "miembros", HttpStatusCode.OK)]
    [InlineData("admin@demo.cl", "inventario", HttpStatusCode.OK)]
    [InlineData("admin@demo.cl", "finanzas", HttpStatusCode.OK)]
    [InlineData("admin@demo.cl", "portal", HttpStatusCode.OK)]
    // Tesorería: finanzas, portal e inventario
    [InlineData("tesoreria@demo.cl", "miembros", HttpStatusCode.Forbidden)]
    [InlineData("tesoreria@demo.cl", "inventario", HttpStatusCode.OK)]
    [InlineData("tesoreria@demo.cl", "finanzas", HttpStatusCode.OK)]
    [InlineData("tesoreria@demo.cl", "portal", HttpStatusCode.OK)]
    // Dirigente: miembros e inventario
    [InlineData("dirigente@demo.cl", "miembros", HttpStatusCode.OK)]
    [InlineData("dirigente@demo.cl", "inventario", HttpStatusCode.OK)]
    [InlineData("dirigente@demo.cl", "finanzas", HttpStatusCode.Forbidden)]
    [InlineData("dirigente@demo.cl", "portal", HttpStatusCode.Forbidden)]
    // Apoderado: solo portal
    [InlineData("apoderado@demo.cl", "miembros", HttpStatusCode.Forbidden)]
    [InlineData("apoderado@demo.cl", "inventario", HttpStatusCode.Forbidden)]
    [InlineData("apoderado@demo.cl", "finanzas", HttpStatusCode.Forbidden)]
    [InlineData("apoderado@demo.cl", "portal", HttpStatusCode.OK)]
    // Revisor: finanzas e inventario (solo lectura)
    [InlineData("revisor@demo.cl", "miembros", HttpStatusCode.Forbidden)]
    [InlineData("revisor@demo.cl", "inventario", HttpStatusCode.OK)]
    [InlineData("revisor@demo.cl", "finanzas", HttpStatusCode.OK)]
    [InlineData("revisor@demo.cl", "portal", HttpStatusCode.Forbidden)]
    public async Task Modulo_SegunRol_PermiteOProhibe(string email, string modulo, HttpStatusCode esperado)
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await ObtenerTokenAsync(cliente, email));

        var respuesta = await cliente.GetAsync($"/api/{modulo}");

        Assert.Equal(esperado, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResponseData<object>>();
        Assert.Equal(esperado == HttpStatusCode.OK, cuerpo!.Exito);
        if (esperado == HttpStatusCode.Forbidden)
        {
            Assert.Equal("No tienes permiso para realizar esta acción.", cuerpo.Mensaje);
        }
    }

    [Theory]
    [InlineData("miembros")]
    [InlineData("inventario")]
    [InlineData("finanzas")]
    [InlineData("portal")]
    public async Task Modulo_SinToken_Devuelve401(string modulo)
    {
        var respuesta = await fabrica.CreateClient().GetAsync($"/api/{modulo}");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private static async Task<string> ObtenerTokenAsync(HttpClient cliente, string email)
    {
        var login = await cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin(email, FabricaApiPruebas.PasswordDemo));
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<ResponseData<RespuestaLogin>>())!.Datos!.Token;
    }
}

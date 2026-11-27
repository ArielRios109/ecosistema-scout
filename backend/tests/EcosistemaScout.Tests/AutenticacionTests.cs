using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcosistemaScout.Api.Modulos.Base.Autenticacion;

namespace EcosistemaScout.Tests;

public class AutenticacionTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    [Fact]
    public async Task Login_ConCredencialesValidas_DevuelveTokenYRoles()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("tesoreria@demo.cl", FabricaApiPruebas.PasswordDemo));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>();
        Assert.NotNull(cuerpo);
        Assert.False(string.IsNullOrWhiteSpace(cuerpo.Token));
        Assert.Equal("tesoreria@demo.cl", cuerpo.Usuario.Email);
        Assert.Contains("tesoreria", cuerpo.Usuario.Roles);
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_Devuelve401()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("admin@demo.cl", "contraseña-incorrecta"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_ConUsuarioInexistente_Devuelve401()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("nadie@demo.cl", FabricaApiPruebas.PasswordDemo));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Me_SinToken_Devuelve401()
    {
        var respuesta = await _cliente.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Me_ConToken_DevuelveUsuarioActual()
    {
        var login = await _cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("dirigente@demo.cl", FabricaApiPruebas.PasswordDemo));
        var token = (await login.Content.ReadFromJsonAsync<RespuestaLogin>())!.Token;

        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioActual>();
        Assert.Equal("dirigente@demo.cl", usuario!.Email);
        Assert.Contains("dirigente", usuario.Roles);
    }
}

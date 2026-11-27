using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
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
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResponseData<RespuestaLogin>>();
        Assert.NotNull(cuerpo);
        Assert.True(cuerpo.Exito);
        Assert.False(string.IsNullOrWhiteSpace(cuerpo.Datos!.Token));
        Assert.Equal("tesoreria@demo.cl", cuerpo.Datos.Usuario.Email);
        Assert.Contains("tesoreria", cuerpo.Datos.Usuario.Roles);
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_Devuelve401ConMensaje()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("admin@demo.cl", "contraseña-incorrecta"));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResponseData<object>>();
        Assert.False(cuerpo!.Exito);
        Assert.Equal("Correo o contraseña incorrectos.", cuerpo.Mensaje);
    }

    [Fact]
    public async Task Login_ConUsuarioInexistente_Devuelve401()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("nadie@demo.cl", FabricaApiPruebas.PasswordDemo));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_ConDatosInvalidos_Devuelve400ConErrores()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", new { email = "no-es-correo", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResponseData<object>>();
        Assert.False(cuerpo!.Exito);
        Assert.Contains("El correo no es válido.", cuerpo.Errores!);
        Assert.Contains("Ingresa tu contraseña.", cuerpo.Errores!);
    }

    [Fact]
    public async Task Me_SinToken_Devuelve401EnFormatoResponseData()
    {
        var respuesta = await _cliente.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResponseData<object>>();
        Assert.False(cuerpo!.Exito);
        Assert.Equal("Debes iniciar sesión para continuar.", cuerpo.Mensaje);
    }

    [Fact]
    public async Task Me_ConToken_DevuelveUsuarioActual()
    {
        var login = await _cliente.PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("dirigente@demo.cl", FabricaApiPruebas.PasswordDemo));
        var token = (await login.Content.ReadFromJsonAsync<ResponseData<RespuestaLogin>>())!.Datos!.Token;

        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResponseData<UsuarioActual>>();
        Assert.Equal("dirigente@demo.cl", cuerpo!.Datos!.Email);
        Assert.Contains("dirigente", cuerpo.Datos.Roles);
    }

    [Fact]
    public async Task RutaInexistente_Devuelve404EnFormatoResponseData()
    {
        var respuesta = await _cliente.GetAsync("/api/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<ResponseData<object>>();
        Assert.False(cuerpo!.Exito);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Base.Autenticacion;
using EcosistemaScout.Api.Modulos.Base.Usuarios;

namespace EcosistemaScout.Tests;

public class UsuariosTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task Listar_ComoAdministrador_DevuelvePaginaConUsuariosDemo()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");

        var pagina = (await (await admin.GetAsync("/api/base/usuarios?tamano=50")).LeerAsync<Pagina<UsuarioResumen>>()).Datos!;

        Assert.True(pagina.Total >= 5);
        Assert.Contains(pagina.Elementos, u => u.Email == "tesoreria@demo.cl" && u.Roles.Contains("tesoreria"));
    }

    [Fact]
    public async Task Listar_ConOtroRol_Devuelve403()
    {
        var tesoreria = await fabrica.ClienteComoAsync("tesoreria@demo.cl");

        var respuesta = await tesoreria.GetAsync("/api/base/usuarios");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearUsuario_ObligaACambiarLaPasswordTemporalAntesDeUsarLaApp()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");

        // 1. El administrador crea la cuenta y recibe una contraseña temporal
        var creacion = await admin.PostAsJsonAsync("/api/base/usuarios",
            new SolicitudCrearUsuario("Nueva Tesorera", "nueva.tesorera@demo.cl", ["tesoreria"]));
        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);
        var temporal = (await creacion.LeerAsync<UsuarioConPasswordTemporal>()).Datos!;
        Assert.True(temporal.Usuario.DebeCambiarPassword);

        // 2. La persona ingresa con la temporal: la API le exige cambiarla
        var cliente = fabrica.CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/auth/login", new SolicitudLogin("nueva.tesorera@demo.cl", temporal.PasswordTemporal));
        var sesion = (await login.LeerAsync<RespuestaLogin>()).Datos!;
        Assert.True(sesion.Usuario.DebeCambiarPassword);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/finanzas")).StatusCode);

        // 3. Cambia la contraseña y recibe un token nuevo que ya funciona
        var cambio = await cliente.PostAsJsonAsync("/api/auth/cambiar-password",
            new SolicitudCambiarPassword(temporal.PasswordTemporal, "MiClaveNueva2026"));
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        var nueva = (await cambio.LeerAsync<RespuestaLogin>()).Datos!;
        Assert.False(nueva.Usuario.DebeCambiarPassword);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nueva.Token);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/finanzas")).StatusCode);
    }

    [Fact]
    public async Task CrearUsuario_ConCorreoRepetido_Devuelve409()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");

        var respuesta = await admin.PostAsJsonAsync("/api/base/usuarios",
            new SolicitudCrearUsuario("Otra", "revisor@demo.cl", ["revisor"]));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearUsuario_ConRolInexistente_Devuelve400()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");

        var respuesta = await admin.PostAsJsonAsync("/api/base/usuarios",
            new SolicitudCrearUsuario("Alguien", "alguien@demo.cl", ["superusuario"]));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task DesactivarUsuario_ImpideIniciarSesion_YReactivarLoPermite()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var creado = (await (await admin.PostAsJsonAsync("/api/base/usuarios",
            new SolicitudCrearUsuario("Temporal", "temporal@demo.cl", ["revisor"]))).LeerAsync<UsuarioConPasswordTemporal>()).Datos!;

        await admin.PostAsync($"/api/base/usuarios/{creado.Usuario.Id}/desactivar", null);
        var login = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("temporal@demo.cl", creado.PasswordTemporal));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        await admin.PostAsync($"/api/base/usuarios/{creado.Usuario.Id}/activar", null);
        login = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("temporal@demo.cl", creado.PasswordTemporal));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Administrador_NoPuedeDesactivarseNiQuitarseElRol()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var yo = (await (await admin.GetAsync("/api/auth/me")).LeerAsync<UsuarioActual>()).Datos!;

        var desactivar = await admin.PostAsync($"/api/base/usuarios/{yo.Id}/desactivar", null);
        var roles = await admin.PutAsJsonAsync($"/api/base/usuarios/{yo.Id}/roles", new SolicitudRoles(["revisor"]));

        Assert.Equal(HttpStatusCode.BadRequest, desactivar.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, roles.StatusCode);
    }

    [Fact]
    public async Task RestablecerPassword_EntregaUnaTemporalQueFunciona()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var creado = (await (await admin.PostAsJsonAsync("/api/base/usuarios",
            new SolicitudCrearUsuario("Olvidadiza", "olvidadiza@demo.cl", ["dirigente"]))).LeerAsync<UsuarioConPasswordTemporal>()).Datos!;

        var reset = (await (await admin.PostAsync($"/api/base/usuarios/{creado.Usuario.Id}/restablecer-password", null))
            .LeerAsync<UsuarioConPasswordTemporal>()).Datos!;

        Assert.NotEqual(creado.PasswordTemporal, reset.PasswordTemporal);
        var login = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login",
            new SolicitudLogin("olvidadiza@demo.cl", reset.PasswordTemporal));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task CambiarPassword_ConPasswordActualIncorrecta_Devuelve400EnEspanol()
    {
        var revisor = await fabrica.ClienteComoAsync("revisor@demo.cl");

        var respuesta = await revisor.PostAsJsonAsync("/api/auth/cambiar-password",
            new SolicitudCambiarPassword("incorrecta", "OtraClave2026"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("La contraseña actual no es correcta.", (await respuesta.LeerAsync<object>()).Errores!);
    }
}

using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Base.Autenticacion;
using EcosistemaScout.Api.Modulos.Miembros.Apoderados;
using EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;

namespace EcosistemaScout.Tests;

public class ApoderadosTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private static int _siguienteRut = 15_000_000;

    private static string NuevoRut()
    {
        var numero = Interlocked.Increment(ref _siguienteRut);
        return $"{numero}-{Rut.CalcularDv(numero)}";
    }

    private static SolicitudApoderado Apoderado(string? email = null) =>
        new(NuevoRut(), "Marcela", "Soto Rojas", email ?? $"apoderado{Guid.NewGuid():N}@correo.cl", "+56 9 1234 5678");

    private async Task<long> NuevoBeneficiarioAsync(HttpClient cliente)
    {
        var unidad = (await (await cliente.PostAsJsonAsync("/api/miembros/unidades",
            new SolicitudUnidad($"Unidad {Guid.NewGuid():N}"[..20], "lobatos"))).LeerAsync<UnidadDto>()).Datos!;
        var beneficiario = (await (await cliente.PostAsJsonAsync("/api/miembros/beneficiarios",
            new SolicitudBeneficiario(NuevoRut(), "Tomás", "Soto", new DateOnly(2017, 2, 1), null, unidad.Id, null, null, null)))
            .LeerAsync<BeneficiarioDetalle>()).Datos!;
        return beneficiario.Id;
    }

    private static async Task<long> CrearAsync(HttpClient cliente, SolicitudApoderado? solicitud = null) =>
        (await (await cliente.PostAsJsonAsync("/api/miembros/apoderados", solicitud ?? Apoderado())).LeerAsync<ApoderadoDetalle>()).Datos!.Id;

    [Fact]
    public async Task Crear_ConRutRepetido_Devuelve409()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var solicitud = Apoderado();
        await CrearAsync(admin, solicitud);

        var repetido = await admin.PostAsJsonAsync("/api/miembros/apoderados", solicitud with { Email = "otro@correo.cl" });

        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
    }

    [Fact]
    public async Task Vincular_ElPrimeroQuedaPrincipal_YSoloHayUnPrincipal()
    {
        var dirigente = await fabrica.ClienteComoAsync("dirigente@demo.cl");
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var beneficiarioId = await NuevoBeneficiarioAsync(admin);
        var madre = await CrearAsync(dirigente);
        var padre = await CrearAsync(dirigente);
        var ruta = $"/api/miembros/beneficiarios/{beneficiarioId}/apoderados";

        await dirigente.PostAsJsonAsync(ruta, new SolicitudVinculo(madre, "Madre", false));
        var vinculos = (await (await dirigente.PostAsJsonAsync(ruta, new SolicitudVinculo(padre, "Padre", true)))
            .LeerAsync<List<VinculoApoderado>>()).Datos!;
        var repetido = await dirigente.PostAsJsonAsync(ruta, new SolicitudVinculo(padre, "Padre", false));

        Assert.Equal(2, vinculos.Count);
        Assert.Equal(padre, Assert.Single(vinculos, v => v.EsPrincipal).ApoderadoId);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
    }

    [Fact]
    public async Task Desvincular_AlPrincipal_PromueveAlSiguiente_YPermiteEliminarSinVinculos()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var beneficiarioId = await NuevoBeneficiarioAsync(admin);
        var principal = await CrearAsync(admin);
        var otro = await CrearAsync(admin);
        var ruta = $"/api/miembros/beneficiarios/{beneficiarioId}/apoderados";
        await admin.PostAsJsonAsync(ruta, new SolicitudVinculo(principal, "Madre", true));
        await admin.PostAsJsonAsync(ruta, new SolicitudVinculo(otro, "Abuela", false));

        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/miembros/apoderados/{principal}")).StatusCode);
        var restantes = (await (await admin.DeleteAsync($"{ruta}/{principal}")).LeerAsync<List<VinculoApoderado>>()).Datos!;

        Assert.True(Assert.Single(restantes).EsPrincipal);
        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/miembros/apoderados/{principal}")).StatusCode);
    }

    [Fact]
    public async Task CrearCuenta_GeneraPasswordTemporal_YPermiteIngresarAlPortal()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var email = $"portal{Guid.NewGuid():N}@correo.cl";
        var id = await CrearAsync(admin, Apoderado(email));

        var cuenta = (await (await admin.PostAsync($"/api/miembros/apoderados/{id}/cuenta", null)).LeerAsync<CuentaPortal>()).Datos!;
        var otraVez = await admin.PostAsync($"/api/miembros/apoderados/{id}/cuenta", null);

        Assert.NotNull(cuenta.PasswordTemporal);
        Assert.Contains("apoderado", cuenta.Usuario.Roles);
        Assert.Equal(HttpStatusCode.Conflict, otraVez.StatusCode);
        var login = await fabrica.CreateClient().PostAsJsonAsync("/api/auth/login", new SolicitudLogin(email, cuenta.PasswordTemporal!));
        Assert.True((await login.LeerAsync<RespuestaLogin>()).Datos!.Usuario.DebeCambiarPassword);
    }

    [Fact]
    public async Task CrearCuenta_NoLaPuedeHacerElDirigente()
    {
        var dirigente = await fabrica.ClienteComoAsync("dirigente@demo.cl");
        var id = await CrearAsync(dirigente);

        Assert.Equal(HttpStatusCode.Forbidden, (await dirigente.PostAsync($"/api/miembros/apoderados/{id}/cuenta", null)).StatusCode);
    }

    [Fact]
    public async Task Listar_BuscaPorNombreCorreoORut()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var solicitud = Apoderado() with { Nombres = "Gertrudis" };
        await CrearAsync(admin, solicitud);

        var porNombre = (await (await admin.GetAsync("/api/miembros/apoderados?buscar=gertrudis")).LeerAsync<Pagina<ApoderadoResumen>>()).Datos!;
        var porRut = (await (await admin.GetAsync($"/api/miembros/apoderados?buscar={Rut.Formatear(solicitud.Rut)}"))
            .LeerAsync<Pagina<ApoderadoResumen>>()).Datos!;

        Assert.Equal("Gertrudis", Assert.Single(porNombre.Elementos).Nombres);
        Assert.Single(porRut.Elementos);
    }
}

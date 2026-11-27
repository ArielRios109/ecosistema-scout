using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;

namespace EcosistemaScout.Tests;

public class UnidadesTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task Administrador_CreaUnidadYPatrulla_YApareceEnElListado()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");

        var unidad = (await (await admin.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("Tropa Pruebas", "scouts")))
            .LeerAsync<UnidadDto>()).Datos!;
        var patrulla = await admin.PostAsJsonAsync($"/api/miembros/unidades/{unidad.Id}/patrullas", new SolicitudPatrulla("Halcones"));
        Assert.Equal(HttpStatusCode.OK, patrulla.StatusCode);

        var lista = (await (await admin.GetAsync("/api/miembros/unidades")).LeerAsync<List<UnidadDto>>()).Datos!;
        var creada = Assert.Single(lista, u => u.Id == unidad.Id);
        Assert.Equal("Halcones", Assert.Single(creada.Patrullas).Nombre);
    }

    [Fact]
    public async Task CrearUnidad_ConRamaInvalida_Devuelve400()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");

        var respuesta = await admin.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("Rara", "dragones"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearUnidad_ConNombreRepetido_Devuelve409()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        await admin.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("Manada Única", "lobatos"));

        var respuesta = await admin.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("manada única", "lobatos"));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Dirigente_VeUnidadesYCreaPatrullas_PeroNoCreaUnidades()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var dirigente = await fabrica.ClienteComoAsync("dirigente@demo.cl");
        var unidad = (await (await admin.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("Compañía Dirigente", "guias")))
            .LeerAsync<UnidadDto>()).Datos!;

        Assert.Equal(HttpStatusCode.OK, (await dirigente.GetAsync("/api/miembros/unidades")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await dirigente.PostAsJsonAsync($"/api/miembros/unidades/{unidad.Id}/patrullas", new SolicitudPatrulla("Golondrinas Azules"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await dirigente.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("No permitida", "scouts"))).StatusCode);
    }

    [Fact]
    public async Task Tesoreria_NoEntraAUnidades()
    {
        var tesoreria = await fabrica.ClienteComoAsync("tesoreria@demo.cl");

        Assert.Equal(HttpStatusCode.Forbidden, (await tesoreria.GetAsync("/api/miembros/unidades")).StatusCode);
    }

    [Fact]
    public async Task UnidadSinDatos_SePuedeEliminar_YDesactivadaNoApareceEnElListado()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var a = (await (await admin.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("Para eliminar", "pioneros"))).LeerAsync<UnidadDto>()).Datos!;
        var b = (await (await admin.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("Para desactivar", "caminantes"))).LeerAsync<UnidadDto>()).Datos!;

        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/miembros/unidades/{a.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/miembros/unidades/{b.Id}/desactivar", null)).StatusCode);

        var activas = (await (await admin.GetAsync("/api/miembros/unidades")).LeerAsync<List<UnidadDto>>()).Datos!;
        var todas = (await (await admin.GetAsync("/api/miembros/unidades?incluirInactivas=true")).LeerAsync<List<UnidadDto>>()).Datos!;
        Assert.DoesNotContain(activas, u => u.Id == a.Id || u.Id == b.Id);
        Assert.Contains(todas, u => u.Id == b.Id && !u.Activa);
    }

    [Fact]
    public async Task UnidadConPatrullas_NoSePuedeEliminar()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var unidad = (await (await admin.PostAsJsonAsync("/api/miembros/unidades", new SolicitudUnidad("Con patrulla", "scouts"))).LeerAsync<UnidadDto>()).Datos!;
        await admin.PostAsJsonAsync($"/api/miembros/unidades/{unidad.Id}/patrullas", new SolicitudPatrulla("Zorros"));

        var respuesta = await admin.DeleteAsync($"/api/miembros/unidades/{unidad.Id}");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;
using EcosistemaScout.Api.Modulos.Miembros.Voluntarios;

namespace EcosistemaScout.Tests;

public class VoluntariosTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private static int _siguienteRut = 13_000_000;
    private static readonly DateOnly Inicio = new(2024, 3, 1);

    private static SolicitudVoluntario Voluntario() =>
        new(NuevoRut(), "Francisca", "Lagos Vera", $"vol{Guid.NewGuid():N}@correo.cl", null);

    private static string NuevoRut()
    {
        var numero = Interlocked.Increment(ref _siguienteRut);
        return $"{numero}-{Rut.CalcularDv(numero)}";
    }

    private static async Task<VoluntarioDetalle> CrearAsync(HttpClient cliente, SolicitudVoluntario? solicitud = null) =>
        (await (await cliente.PostAsJsonAsync("/api/miembros/voluntarios", solicitud ?? Voluntario())).LeerAsync<VoluntarioDetalle>()).Datos!;

    [Fact]
    public async Task AsignarYTerminarCargo_QuedaEnElHistorial()
    {
        var dirigente = await fabrica.ClienteComoAsync("dirigente@demo.cl");
        var voluntario = await CrearAsync(dirigente);
        var ruta = $"/api/miembros/voluntarios/{voluntario.Id}/cargos";

        var conCargo = (await (await dirigente.PostAsJsonAsync(ruta, new SolicitudCargo("Jefa de grupo", null, Inicio, null)))
            .LeerAsync<VoluntarioDetalle>()).Datos!;
        var cargo = Assert.Single(conCargo.Cargos);
        Assert.True(cargo.Vigente);

        var terminado = (await (await dirigente.PostAsJsonAsync($"{ruta}/{cargo.Id}/terminar",
            new SolicitudTerminarCargo(new DateOnly(2025, 12, 31)))).LeerAsync<VoluntarioDetalle>()).Datos!;
        Assert.False(Assert.Single(terminado.Cargos).Vigente);
        Assert.Equal(HttpStatusCode.Conflict,
            (await dirigente.PostAsJsonAsync($"{ruta}/{cargo.Id}/terminar", new SolicitudTerminarCargo(null))).StatusCode);
    }

    [Fact]
    public async Task Cargo_ConFechasIncoherentes_Devuelve400()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var voluntario = await CrearAsync(admin);
        var ruta = $"/api/miembros/voluntarios/{voluntario.Id}/cargos";

        var termino = await admin.PostAsJsonAsync(ruta, new SolicitudCargo("Asistente", null, Inicio, Inicio.AddDays(-1)));
        var futuro = await admin.PostAsJsonAsync(ruta, new SolicitudCargo("Asistente", null, DateOnly.FromDateTime(DateTime.Today).AddDays(5), null));

        Assert.Equal(HttpStatusCode.BadRequest, termino.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, futuro.StatusCode);
    }

    [Fact]
    public async Task Listar_FiltraPorUnidadDelCargoVigente_YMuestraSoloCargosVigentes()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var unidad = (await (await admin.PostAsJsonAsync("/api/miembros/unidades",
            new SolicitudUnidad($"Unidad {Guid.NewGuid():N}"[..20], "caminantes"))).LeerAsync<UnidadDto>()).Datos!;
        var voluntario = await CrearAsync(admin);
        var ruta = $"/api/miembros/voluntarios/{voluntario.Id}/cargos";
        await admin.PostAsJsonAsync(ruta, new SolicitudCargo("Jefe de unidad", unidad.Id, Inicio, null));
        await admin.PostAsJsonAsync(ruta, new SolicitudCargo("Tesorero", null, new DateOnly(2020, 1, 1), new DateOnly(2021, 1, 1)));

        var pagina = (await (await admin.GetAsync($"/api/miembros/voluntarios?unidadId={unidad.Id}"))
            .LeerAsync<Pagina<VoluntarioResumen>>()).Datos!;

        var encontrado = Assert.Single(pagina.Elementos);
        var vigente = Assert.Single(encontrado.CargosVigentes);
        Assert.Equal("Jefe de unidad", vigente.Cargo);
        Assert.Equal(unidad.Nombre, vigente.Unidad);
    }

    [Fact]
    public async Task Eliminar_SoloSinCargos()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var sinCargos = await CrearAsync(admin);
        var conCargos = await CrearAsync(admin);
        await admin.PostAsJsonAsync($"/api/miembros/voluntarios/{conCargos.Id}/cargos", new SolicitudCargo("Apoyo", null, Inicio, null));

        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/miembros/voluntarios/{sinCargos.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/miembros/voluntarios/{conCargos.Id}")).StatusCode);
    }

    [Fact]
    public async Task Crear_ConRutRepetido_Devuelve409()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var solicitud = Voluntario();
        await CrearAsync(admin, solicitud);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/miembros/voluntarios", solicitud)).StatusCode);
    }
}

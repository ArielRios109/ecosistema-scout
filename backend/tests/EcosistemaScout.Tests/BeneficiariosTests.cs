using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;

namespace EcosistemaScout.Tests;

public class BeneficiariosTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private static int _siguienteRut = 21_000_000;

    /// <summary>RUT válido y distinto en cada llamada (las pruebas comparten la misma base).</summary>
    private static string NuevoRut()
    {
        var numero = Interlocked.Increment(ref _siguienteRut);
        return $"{numero}-{Rut.CalcularDv(numero)}";
    }

    private static SolicitudBeneficiario Solicitud(long unidadId, long? patrullaId = null, string? rut = null) =>
        new(rut ?? NuevoRut(), "Camila", "Pérez Soto", new DateOnly(2014, 5, 10), "Pasaje Los Aromos 123",
            unidadId, patrullaId, new DateOnly(2025, 3, 1), null);

    private async Task<(HttpClient Cliente, UnidadDto Unidad)> PrepararAsync(string usuario = "admin@demo.cl")
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var unidad = (await (await admin.PostAsJsonAsync("/api/miembros/unidades",
            new SolicitudUnidad($"Unidad {Guid.NewGuid():N}"[..20], "scouts"))).LeerAsync<UnidadDto>()).Datos!;
        var cliente = usuario == "admin@demo.cl" ? admin : await fabrica.ClienteComoAsync(usuario);
        return (cliente, unidad);
    }

    [Fact]
    public async Task Crear_RegistraBeneficiarioConRutFormateado_YMovimientoDeIngreso()
    {
        var (cliente, unidad) = await PrepararAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios", Solicitud(unidad.Id, rut: "12.345.678-5"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = (await respuesta.LeerAsync<BeneficiarioDetalle>()).Datos!;
        Assert.Equal("12.345.678-5", creado.Rut);
        Assert.Equal(unidad.Nombre, creado.Unidad);

        var historial = (await (await cliente.GetAsync($"/api/miembros/beneficiarios/{creado.Id}/historial"))
            .LeerAsync<List<MovimientoHistorial>>()).Datos!;
        var ingreso = Assert.Single(historial);
        Assert.Equal(TiposHistorial.Ingreso, ingreso.Tipo);
        Assert.Equal(unidad.Nombre, ingreso.UnidadDestino);
        Assert.NotNull(ingreso.Usuario);
    }

    [Fact]
    public async Task Crear_ConRutInvalidoORepetido_SeRechaza()
    {
        var (cliente, unidad) = await PrepararAsync();
        var rut = NuevoRut();
        await cliente.PostAsJsonAsync("/api/miembros/beneficiarios", Solicitud(unidad.Id, rut: rut));

        var invalido = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios", Solicitud(unidad.Id, rut: "12.345.678-9"));
        var repetido = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios", Solicitud(unidad.Id, rut: rut));

        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
    }

    [Fact]
    public async Task Crear_ConPatrullaDeOtraUnidad_Devuelve400()
    {
        var (cliente, unidad) = await PrepararAsync();
        var (_, otra) = await PrepararAsync();
        var patrulla = (await (await cliente.PostAsJsonAsync($"/api/miembros/unidades/{otra.Id}/patrullas",
            new SolicitudPatrulla("Ajena"))).LeerAsync<PatrullaDto>()).Datos!;

        var respuesta = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios", Solicitud(unidad.Id, patrulla.Id));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task CambioDeUnidadYPatrulla_RetiroYReingreso_QuedanEnElHistorial()
    {
        var (cliente, unidad) = await PrepararAsync("dirigente@demo.cl");
        var (_, destino) = await PrepararAsync();
        var patrulla = (await (await cliente.PostAsJsonAsync($"/api/miembros/unidades/{destino.Id}/patrullas",
            new SolicitudPatrulla("Cóndores"))).LeerAsync<PatrullaDto>()).Datos!;
        var solicitud = Solicitud(unidad.Id);
        var creado = (await (await cliente.PostAsJsonAsync("/api/miembros/beneficiarios", solicitud))
            .LeerAsync<BeneficiarioDetalle>()).Datos!;

        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync($"/api/miembros/beneficiarios/{creado.Id}",
            solicitud with { UnidadId = destino.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync($"/api/miembros/beneficiarios/{creado.Id}",
            solicitud with { UnidadId = destino.Id, PatrullaId = patrulla.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync($"/api/miembros/beneficiarios/{creado.Id}/retirar",
            new SolicitudMovimiento(null, "Cambio de ciudad"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync($"/api/miembros/beneficiarios/{creado.Id}/retirar",
            new SolicitudMovimiento(null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsJsonAsync($"/api/miembros/beneficiarios/{creado.Id}/reingresar",
            new SolicitudMovimiento(null, "Regresa al grupo"))).StatusCode);

        var historial = (await (await cliente.GetAsync($"/api/miembros/beneficiarios/{creado.Id}/historial"))
            .LeerAsync<List<MovimientoHistorial>>()).Datos!;
        Assert.Equal(
            [TiposHistorial.Ingreso, TiposHistorial.CambioUnidad, TiposHistorial.CambioPatrulla, TiposHistorial.Retiro, TiposHistorial.Reingreso],
            historial.OrderBy(h => h.Id).Select(h => h.Tipo));
        Assert.Equal("De sin patrulla a Cóndores.", historial.Single(h => h.Tipo == TiposHistorial.CambioPatrulla).Detalle);
    }

    [Fact]
    public async Task Listar_FiltraPorUnidadEstadoYBusqueda()
    {
        var (cliente, unidad) = await PrepararAsync();
        var rut = NuevoRut();
        await cliente.PostAsJsonAsync("/api/miembros/beneficiarios", Solicitud(unidad.Id, rut: rut) with { Nombres = "Tomás" });
        var retirado = (await (await cliente.PostAsJsonAsync("/api/miembros/beneficiarios", Solicitud(unidad.Id)))
            .LeerAsync<BeneficiarioDetalle>()).Datos!;
        await cliente.PostAsJsonAsync($"/api/miembros/beneficiarios/{retirado.Id}/retirar", new SolicitudMovimiento(null, null));

        var activos = (await (await cliente.GetAsync($"/api/miembros/beneficiarios?unidadId={unidad.Id}&estado=activo"))
            .LeerAsync<Pagina<BeneficiarioResumen>>()).Datos!;
        var porNombre = (await (await cliente.GetAsync($"/api/miembros/beneficiarios?unidadId={unidad.Id}&buscar=tomás"))
            .LeerAsync<Pagina<BeneficiarioResumen>>()).Datos!;
        var porRut = (await (await cliente.GetAsync($"/api/miembros/beneficiarios?buscar={Rut.Formatear(rut)}"))
            .LeerAsync<Pagina<BeneficiarioResumen>>()).Datos!;

        Assert.Equal(1, activos.Total);
        Assert.Equal("Tomás", Assert.Single(porNombre.Elementos).Nombres);
        Assert.Equal(Rut.Formatear(rut), Assert.Single(porRut.Elementos).Rut);
    }

    [Fact]
    public async Task Tesoreria_NoAccedeABeneficiarios()
    {
        var tesoreria = await fabrica.ClienteComoAsync("tesoreria@demo.cl");

        Assert.Equal(HttpStatusCode.Forbidden, (await tesoreria.GetAsync("/api/miembros/beneficiarios")).StatusCode);
    }

    [Theory]
    [InlineData(2010, 6, 15, 2026, 6, 14, 15)]
    [InlineData(2010, 6, 15, 2026, 6, 15, 16)]
    public void Edad_SeCalculaSegunElCumpleanos(int a, int m, int d, int ha, int hm, int hd, int esperada) =>
        Assert.Equal(esperada, BeneficiariosController.Edad(new DateOnly(a, m, d), new DateOnly(ha, hm, hd)));
}

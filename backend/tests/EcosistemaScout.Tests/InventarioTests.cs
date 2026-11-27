using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcosistemaScout.Api.Modulos.Inventario;

namespace EcosistemaScout.Tests;

public class InventarioTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(DateTime.Today);

    private async Task<(HttpClient Cliente, BienDetalle Bien)> NuevoBienAsync(int cantidad = 4)
    {
        var tesoreria = await fabrica.ClienteComoAsync("tesoreria@demo.cl");
        var categoria = (await (await tesoreria.PostAsJsonAsync("/api/inventario/categorias",
            new SolicitudCategoria($"Categoría {Guid.NewGuid():N}"[..20]))).LeerAsync<CategoriaDto>()).Datos!;
        var bien = (await (await tesoreria.PostAsJsonAsync("/api/inventario/bienes", new SolicitudBien(
            "Carpa de prueba", categoria.Id, cantidad, null, null, null, null, null, null, "propio", "bueno", "Bodega", null)))
            .LeerAsync<BienDetalle>()).Datos!;
        return (tesoreria, bien);
    }

    [Fact]
    public async Task Prestamo_NoSuperaLoDisponible_YLaPerdidaDescuentaLaCantidad()
    {
        var (cliente, bien) = await NuevoBienAsync(4);
        Assert.StartsWith("INV-", bien.Codigo);

        var prestamo = (await (await cliente.PostAsJsonAsync($"/api/inventario/bienes/{bien.Id}/prestamos",
            new SolicitudPrestamo(3, "Tropa", "Campamento", null, Hoy.AddDays(5)))).LeerAsync<PrestamoDto>()).Datos!;
        var demasiado = await cliente.PostAsJsonAsync($"/api/inventario/bienes/{bien.Id}/prestamos",
            new SolicitudPrestamo(2, "Manada", null, null, Hoy.AddDays(5)));
        await cliente.PostAsJsonAsync($"/api/inventario/prestamos/{prestamo.Id}/devolucion", new SolicitudDevolucion(null, "perdido"));
        var detalle = (await (await cliente.GetAsync($"/api/inventario/bienes/{bien.Id}")).LeerAsync<BienDetalle>()).Datos!;

        Assert.Equal(HttpStatusCode.Conflict, demasiado.StatusCode);
        Assert.Equal(1, detalle.Cantidad);
        Assert.Equal(1, detalle.Disponible);
    }

    [Fact]
    public async Task Baja_ConPrestamoActivo_SeRechaza_YSinPrestamosQuedaEnHistorial()
    {
        var (cliente, bien) = await NuevoBienAsync();
        var prestamo = (await (await cliente.PostAsJsonAsync($"/api/inventario/bienes/{bien.Id}/prestamos",
            new SolicitudPrestamo(1, "Clan", null, null, Hoy.AddDays(1)))).LeerAsync<PrestamoDto>()).Datos!;

        var conPrestamo = await cliente.PostAsJsonAsync($"/api/inventario/bienes/{bien.Id}/baja", new SolicitudBaja("deterioro", null, null));
        await cliente.PostAsJsonAsync($"/api/inventario/prestamos/{prestamo.Id}/devolucion", new SolicitudDevolucion(null, "bueno"));
        var baja = (await (await cliente.PostAsJsonAsync($"/api/inventario/bienes/{bien.Id}/baja",
            new SolicitudBaja("deterioro", null, "Roto"))).LeerAsync<BienDetalle>()).Datos!;

        Assert.Equal(HttpStatusCode.Conflict, conPrestamo.StatusCode);
        Assert.True(baja.DadoDeBaja);
        Assert.Equal("deterioro", baja.Baja!.Motivo);
        Assert.Single(baja.Prestamos);
    }

    [Fact]
    public async Task Dirigente_PrestaPeroNoAdministraBienes_YRevisorSoloConsulta()
    {
        var (_, bien) = await NuevoBienAsync();
        var dirigente = await fabrica.ClienteComoAsync("dirigente@demo.cl");
        var revisor = await fabrica.ClienteComoAsync("revisor@demo.cl");

        Assert.Equal(HttpStatusCode.OK, (await dirigente.PostAsJsonAsync($"/api/inventario/bienes/{bien.Id}/prestamos",
            new SolicitudPrestamo(1, "Compañía", null, null, Hoy.AddDays(2)))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await dirigente.PostAsJsonAsync($"/api/inventario/bienes/{bien.Id}/baja",
            new SolicitudBaja("otro", null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await revisor.GetAsync("/api/inventario/bienes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await revisor.PostAsJsonAsync($"/api/inventario/bienes/{bien.Id}/prestamos",
            new SolicitudPrestamo(1, "X", null, null, Hoy))).StatusCode);
    }

    [Fact]
    public async Task Foto_SoloAceptaImagenesReales_YExportarEntregaExcel()
    {
        var (cliente, bien) = await NuevoBienAsync();
        var falsa = new MultipartFormDataContent();
        var texto = new ByteArrayContent("esto no es una imagen"u8.ToArray());
        texto.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        falsa.Add(texto, "foto", "foto.png");
        var png = new MultipartFormDataContent();
        var imagen = new ByteArrayContent(Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="));
        imagen.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        png.Add(imagen, "foto", "foto.png");

        var rechazada = await cliente.PutAsync($"/api/inventario/bienes/{bien.Id}/foto", falsa);
        var aceptada = await cliente.PutAsync($"/api/inventario/bienes/{bien.Id}/foto", png);
        var foto = await cliente.GetAsync($"/api/inventario/bienes/{bien.Id}/foto");
        var excel = await cliente.GetAsync("/api/inventario/bienes/exportar");

        Assert.Equal(HttpStatusCode.BadRequest, rechazada.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aceptada.StatusCode);
        Assert.Equal("image/png", foto.Content.Headers.ContentType!.MediaType);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excel.Content.Headers.ContentType!.MediaType);
    }
}

using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;
using EcosistemaScout.Api.Modulos.Miembros.Contactos;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;

namespace EcosistemaScout.Tests;

public class ContactosTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private static int _siguienteRut = 17_000_000;

    private async Task<(HttpClient Cliente, string Ruta)> PrepararAsync()
    {
        var cliente = await fabrica.ClienteComoAsync("admin@demo.cl");
        var unidad = (await (await cliente.PostAsJsonAsync("/api/miembros/unidades",
            new SolicitudUnidad($"Unidad {Guid.NewGuid():N}"[..20], "guias"))).LeerAsync<UnidadDto>()).Datos!;
        var numero = Interlocked.Increment(ref _siguienteRut);
        var beneficiario = (await (await cliente.PostAsJsonAsync("/api/miembros/beneficiarios",
            new SolicitudBeneficiario($"{numero}-{Rut.CalcularDv(numero)}", "Josefa", "Muñoz", new DateOnly(2012, 8, 20),
                null, unidad.Id, null, null, null))).LeerAsync<BeneficiarioDetalle>()).Datos!;
        return (cliente, $"/api/miembros/beneficiarios/{beneficiario.Id}/contactos");
    }

    [Fact]
    public async Task Agregar_EditarYEliminar_Contacto()
    {
        var (cliente, ruta) = await PrepararAsync();

        var lista = (await (await cliente.PostAsJsonAsync(ruta, new SolicitudContacto("Ana Muñoz", "Tía", "+56 9 8765 4321")))
            .LeerAsync<List<ContactoDto>>()).Datos!;
        var id = Assert.Single(lista).Id;
        var editada = (await (await cliente.PutAsJsonAsync($"{ruta}/{id}", new SolicitudContacto("Ana Muñoz", "Madrina", "+56 9 8765 4321")))
            .LeerAsync<List<ContactoDto>>()).Datos!;
        var vacia = (await (await cliente.DeleteAsync($"{ruta}/{id}")).LeerAsync<List<ContactoDto>>()).Datos!;

        Assert.Equal("Madrina", Assert.Single(editada).Parentesco);
        Assert.Empty(vacia);
    }

    [Fact]
    public async Task Agregar_ConTelefonoInvalido_Devuelve400()
    {
        var (cliente, ruta) = await PrepararAsync();

        var respuesta = await cliente.PostAsJsonAsync(ruta, new SolicitudContacto("Pedro", null, "llamar al vecino"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Agregar_MasDelMaximo_Devuelve409()
    {
        var (cliente, ruta) = await PrepararAsync();
        for (var i = 0; i < ContactosController.Maximo; i++)
        {
            await cliente.PostAsJsonAsync(ruta, new SolicitudContacto($"Contacto {i}", null, $"+56 9 1111 222{i}"));
        }

        var respuesta = await cliente.PostAsJsonAsync(ruta, new SolicitudContacto("Uno más", null, "+56 9 3333 4444"));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }
}

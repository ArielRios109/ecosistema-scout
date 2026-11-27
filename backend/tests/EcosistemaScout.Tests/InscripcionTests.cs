using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Miembros.Apoderados;
using EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;
using EcosistemaScout.Api.Modulos.Miembros.Contactos;
using EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcosistemaScout.Tests;

public class InscripcionTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private static int _siguienteRut = 11_000_000;

    private static string NuevoRut()
    {
        var numero = Interlocked.Increment(ref _siguienteRut);
        return $"{numero}-{Rut.CalcularDv(numero)}";
    }

    private async Task<(HttpClient Cliente, long UnidadId)> PrepararAsync()
    {
        var cliente = await fabrica.ClienteComoAsync("dirigente@demo.cl");
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var unidad = (await (await admin.PostAsJsonAsync("/api/miembros/unidades",
            new SolicitudUnidad($"Unidad {Guid.NewGuid():N}"[..20], "scouts"))).LeerAsync<UnidadDto>()).Datos!;
        return (cliente, unidad.Id);
    }

    private static SolicitudBeneficiario Beneficiario(long unidadId, string? rut = null) =>
        new(rut ?? NuevoRut(), "Martina", "Fuentes Lara", new DateOnly(2013, 9, 2), null, unidadId, null, null, null);

    private static SolicitudApoderado ApoderadoNuevo(string? rut = null) =>
        new(rut ?? NuevoRut(), "Paula", "Lara Pinto", $"paula{Guid.NewGuid():N}@correo.cl", "+56 9 4444 5555");

    [Fact]
    public async Task Inscripcion_Completa_GuardaBeneficiarioApoderadosContactosYFicha()
    {
        var (cliente, unidadId) = await PrepararAsync();
        var existente = (await (await cliente.PostAsJsonAsync("/api/miembros/apoderados", ApoderadoNuevo()))
            .LeerAsync<ApoderadoDetalle>()).Datos!;

        var respuesta = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios/inscripcion", new SolicitudInscripcion(
            Beneficiario(unidadId),
            [new ApoderadoInscripcion(existente.Id, null, "Padre", false), new ApoderadoInscripcion(null, ApoderadoNuevo(), "Madre", true)],
            [new SolicitudContacto("Rosa Lara", "Abuela", "+56 9 1212 3434")],
            new SolicitudFichaMedica("Fonasa", "O+", "Penicilina", null, null, null, null)));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var id = (await respuesta.LeerAsync<BeneficiarioDetalle>()).Datos!.Id;
        var vinculos = (await (await cliente.GetAsync($"/api/miembros/beneficiarios/{id}/apoderados"))
            .LeerAsync<List<VinculoApoderado>>()).Datos!;
        Assert.Equal(2, vinculos.Count);
        Assert.Equal("Madre", Assert.Single(vinculos, v => v.EsPrincipal).Parentesco);
        var contactos = (await (await cliente.GetAsync($"/api/miembros/beneficiarios/{id}/contactos"))
            .LeerAsync<List<ContactoDto>>()).Datos!;
        Assert.Single(contactos);
        var ficha = (await (await cliente.GetAsync($"/api/miembros/beneficiarios/{id}/ficha-medica"))
            .LeerAsync<FichaMedicaDto>()).Datos!;
        Assert.Equal("Penicilina", ficha.Alergias);
        var historial = (await (await cliente.GetAsync($"/api/miembros/beneficiarios/{id}/historial"))
            .LeerAsync<List<MovimientoHistorial>>()).Datos!;
        Assert.Equal(TiposHistorial.Ingreso, Assert.Single(historial).Tipo);
    }

    [Fact]
    public async Task Inscripcion_ConApoderadoNuevoYaRegistrado_NoGuardaNada()
    {
        var (cliente, unidadId) = await PrepararAsync();
        var rutApoderado = NuevoRut();
        await cliente.PostAsJsonAsync("/api/miembros/apoderados", ApoderadoNuevo(rutApoderado));
        var rutBeneficiario = NuevoRut();

        var respuesta = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios/inscripcion", new SolicitudInscripcion(
            Beneficiario(unidadId, rutBeneficiario),
            [new ApoderadoInscripcion(null, ApoderadoNuevo(rutApoderado), "Madre", true)],
            null, null));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var existe = (await (await cliente.GetAsync($"/api/miembros/beneficiarios/existe?rut={rutBeneficiario}"))
            .LeerAsync<RutRegistrado>()).Datos!;
        Assert.False(existe.Existe);
    }

    [Fact]
    public async Task Inscripcion_SinApoderadoNiContactos_EsValida_YConDatosIncoherentesSeRechaza()
    {
        var (cliente, unidadId) = await PrepararAsync();

        var simple = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios/inscripcion",
            new SolicitudInscripcion(Beneficiario(unidadId), null, null, null));
        var ambos = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios/inscripcion",
            new SolicitudInscripcion(Beneficiario(unidadId), [new ApoderadoInscripcion(1, ApoderadoNuevo(), "Madre", true)], null, null));
        var demasiados = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios/inscripcion",
            new SolicitudInscripcion(Beneficiario(unidadId), null,
                Enumerable.Range(0, 6).Select(i => new SolicitudContacto($"Contacto {i}", null, $"+56 9 5555 000{i}")).ToList(), null));

        Assert.Equal(HttpStatusCode.Created, simple.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, ambos.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, demasiados.StatusCode);
    }

    [Theory]
    [InlineData("beneficiarios")]
    [InlineData("apoderados")]
    [InlineData("voluntarios")]
    public async Task Existe_InformaSiElRutEstaRegistrado(string recurso)
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        using var scope = fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rutRegistrado = recurso switch
        {
            "beneficiarios" => await db.Beneficiarios.Select(b => b.Rut).FirstAsync(),
            "apoderados" => await db.Apoderados.Select(a => a.Rut).FirstAsync(),
            _ => await db.Voluntarios.Select(v => v.Rut).FirstAsync(),
        };

        var registrado = (await (await admin.GetAsync($"/api/miembros/{recurso}/existe?rut={Rut.Formatear(rutRegistrado)}"))
            .LeerAsync<RutRegistrado>()).Datos!;
        var libre = (await (await admin.GetAsync($"/api/miembros/{recurso}/existe?rut={NuevoRut()}"))
            .LeerAsync<RutRegistrado>()).Datos!;
        var invalido = await admin.GetAsync($"/api/miembros/{recurso}/existe?rut=12.345.678-9");

        Assert.True(registrado.Existe);
        Assert.NotNull(registrado.Nombre);
        Assert.False(libre.Existe);
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Fact]
    public async Task CamposDemasiadoLargos_SeRechazanConMensajeEnEspanol()
    {
        var (cliente, unidadId) = await PrepararAsync();

        var respuesta = await cliente.PostAsJsonAsync("/api/miembros/beneficiarios",
            Beneficiario(unidadId) with { Nombres = new string('a', 81) });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Los nombres admiten hasta 80 caracteres.", (await respuesta.LeerAsync<object>()).Errores!);
    }
}

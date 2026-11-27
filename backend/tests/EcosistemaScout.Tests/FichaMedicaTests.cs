using System.Net;
using System.Net.Http.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;
using EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcosistemaScout.Tests;

public class FichaMedicaTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private static int _siguienteRut = 19_000_000;

    private static readonly SolicitudFichaMedica Ficha = new(
        "Fonasa", "o+", "Penicilina", "Asma leve", "Inhalador salbutamol", "Celíaco", "Se angustia de noche lejos de casa");

    private async Task<(long Id, string Ruta)> NuevoBeneficiarioAsync()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var unidad = (await (await admin.PostAsJsonAsync("/api/miembros/unidades",
            new SolicitudUnidad($"Unidad {Guid.NewGuid():N}"[..20], "pioneros"))).LeerAsync<UnidadDto>()).Datos!;
        var numero = Interlocked.Increment(ref _siguienteRut);
        var beneficiario = (await (await admin.PostAsJsonAsync("/api/miembros/beneficiarios",
            new SolicitudBeneficiario($"{numero}-{Rut.CalcularDv(numero)}", "Benjamín", "Rojas", new DateOnly(2010, 1, 15),
                null, unidad.Id, null, null, null))).LeerAsync<BeneficiarioDetalle>()).Datos!;
        return (beneficiario.Id, $"/api/miembros/beneficiarios/{beneficiario.Id}/ficha-medica");
    }

    [Fact]
    public async Task SinFicha_DevuelveExisteFalse()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var (_, ruta) = await NuevoBeneficiarioAsync();

        var ficha = (await (await admin.GetAsync(ruta)).LeerAsync<FichaMedicaDto>()).Datos!;

        Assert.False(ficha.Existe);
    }

    [Fact]
    public async Task Dirigente_GuardaYLeeLaFicha_ConAutorYFecha()
    {
        var dirigente = await fabrica.ClienteComoAsync("dirigente@demo.cl");
        var (_, ruta) = await NuevoBeneficiarioAsync();

        var put = await dirigente.PutAsJsonAsync(ruta, Ficha);
        Assert.True(put.IsSuccessStatusCode, await put.Content.ReadAsStringAsync());
        var ficha = (await (await dirigente.GetAsync(ruta)).LeerAsync<FichaMedicaDto>()).Datos!;

        Assert.True(ficha.Existe);
        Assert.Equal("Penicilina", ficha.Alergias);
        Assert.Equal("O+", ficha.GrupoSanguineo);
        Assert.NotNull(ficha.ActualizadaPor);
        Assert.NotNull(ficha.ActualizadaEl);
    }

    [Fact]
    public async Task LaFicha_SeGuardaCifrada_YLaBitacoraNoMuestraLosDatos()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var (id, ruta) = await NuevoBeneficiarioAsync();

        await admin.PutAsJsonAsync(ruta, Ficha);

        using var scope = fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var guardada = await db.FichasMedicas.AsNoTracking().SingleAsync(f => f.BeneficiarioId == id);
        Assert.NotNull(guardada.Alergias);
        Assert.DoesNotContain("Penicilina", guardada.Alergias);
        Assert.DoesNotContain("Asma", guardada.Enfermedades);

        var registro = await db.Bitacora.AsNoTracking()
            .Where(b => b.Entidad == "fichas_medicas" && b.EntidadId == guardada.Id).SingleAsync();
        Assert.DoesNotContain("Penicilina", registro.Despues);
        Assert.Contains("(dato protegido)", registro.Despues);
    }

    [Fact]
    public async Task GrupoSanguineoInvalido_Devuelve400()
    {
        var admin = await fabrica.ClienteComoAsync("admin@demo.cl");
        var (_, ruta) = await NuevoBeneficiarioAsync();

        var respuesta = await admin.PutAsJsonAsync(ruta, Ficha with { GrupoSanguineo = "Z9" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("tesoreria@demo.cl")]
    [InlineData("apoderado@demo.cl")]
    [InlineData("revisor@demo.cl")]
    public async Task OtrosRoles_NoAccedenALaFicha(string usuario)
    {
        var cliente = await fabrica.ClienteComoAsync(usuario);
        var (_, ruta) = await NuevoBeneficiarioAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(ruta)).StatusCode);
    }
}

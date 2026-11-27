using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcosistemaScout.Tests;

public class DatosDemoTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task DatosDemo_TienenRutValidos_CuentasVinculadas_YFichasCifradas()
    {
        using var scope = fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cifrado = scope.ServiceProvider.GetRequiredService<CifradoFichaMedica>();

        var unidad = await db.Unidades.SingleAsync(u => u.Nombre == DatosDemoMiembros.UnidadMarcador);
        var ruts = await db.Beneficiarios.Select(b => b.Rut).ToListAsync();
        var apoderadoDemo = await db.Apoderados.SingleAsync(a => a.Email == "apoderado@demo.cl");
        var voluntarioDemo = await db.Voluntarios.SingleAsync(v => v.Email == "dirigente@demo.cl");
        var ficha = await db.FichasMedicas.FirstAsync(f => f.Alergias != null);

        Assert.True(ruts.Count >= 20);
        Assert.All(ruts, rut => Assert.True(Rut.EsValido(rut), $"RUT inválido: {rut}"));
        Assert.NotNull(apoderadoDemo.UsuarioId);
        Assert.NotNull(voluntarioDemo.UsuarioId);
        Assert.NotEqual(unidad.Id, 0);
        Assert.NotNull(cifrado.Descifrar(ficha.Alergias));
    }

    [Fact]
    public async Task DatosDemo_SonIdempotentes()
    {
        using var scope = fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var antes = await db.Beneficiarios.CountAsync();

        await scope.ServiceProvider.GetRequiredService<DatosDemoMiembros>().CargarAsync();

        Assert.Equal(antes, await db.Beneficiarios.CountAsync());
    }
}

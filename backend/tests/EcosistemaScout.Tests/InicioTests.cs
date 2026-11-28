using EcosistemaScout.Api.Modulos.Base.Inicio;

namespace EcosistemaScout.Tests;

public class InicioTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    private async Task<ResumenInicio> ResumenDeAsync(string usuario)
    {
        var cliente = await fabrica.ClienteComoAsync(usuario);
        return (await (await cliente.GetAsync("/api/base/inicio")).LeerAsync<ResumenInicio>()).Datos!;
    }

    [Fact]
    public async Task Administrador_VeTodoElGrupo_YDirigenteSoloSuUnidad()
    {
        var admin = await ResumenDeAsync("admin@demo.cl");
        var dirigente = await ResumenDeAsync("dirigente@demo.cl");

        Assert.Equal("Todo el grupo", admin.Miembros!.Alcance);
        Assert.NotNull(admin.Inventario);
        Assert.NotNull(admin.Usuarios);
        Assert.True(admin.FinanzasProximamente);
        // En los datos demo, el dirigente es responsable de la Tropa Lautaro.
        Assert.Equal("Tropa Lautaro", dirigente.Miembros!.Alcance);
        Assert.Single(dirigente.Miembros.Unidades);
        Assert.Null(dirigente.Usuarios);
    }

    [Fact]
    public async Task Apoderado_SoloVeASusBeneficiarios()
    {
        var apoderado = await ResumenDeAsync("apoderado@demo.cl");

        Assert.Null(apoderado.Miembros);
        Assert.Null(apoderado.Inventario);
        Assert.NotEmpty(apoderado.MisBeneficiarios!);
    }
}

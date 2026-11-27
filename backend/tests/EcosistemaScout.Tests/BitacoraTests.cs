using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Miembros;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcosistemaScout.Tests;

public class BitacoraTests(FabricaApiPruebas fabrica) : IClassFixture<FabricaApiPruebas>
{
    [Fact]
    public async Task CrearModificarYEliminar_QuedanRegistradosEnLaBitacora()
    {
        using var scope = fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unidad = new Unidad { Nombre = "Tropa Bitácora", Rama = "scouts" };
        db.Unidades.Add(unidad);
        await db.SaveChangesAsync();

        unidad.Nombre = "Tropa Bitácora 2";
        await db.SaveChangesAsync();

        db.Unidades.Remove(unidad);
        await db.SaveChangesAsync();

        var registros = await db.Bitacora
            .Where(b => b.Entidad == "unidades" && b.EntidadId == unidad.Id)
            .OrderBy(b => b.Id)
            .ToListAsync();

        Assert.Equal(["crear", "modificar", "eliminar"], registros.Select(r => r.Accion));
        Assert.Contains("Tropa Bitácora 2", registros[1].Despues);
        Assert.Contains("Tropa Bitácora", registros[1].Antes);
        Assert.Null(registros[2].Despues);
    }
}

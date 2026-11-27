using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcosistemaScout.Api.Datos;

/// <summary>
/// Permite a "dotnet ef migrations add" construir el modelo sin levantar la API ni necesitar secretos.
/// La cadena de conexión no se usa para conectarse; solo indica el proveedor (PostgreSQL).
/// </summary>
public class FabricaDisenoDbContext : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=ecosistema_scout_diseno")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(opciones);
    }
}

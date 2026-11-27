using EcosistemaScout.Api.Datos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EcosistemaScout.Tests;

/// <summary>
/// Levanta la API completa en memoria para las pruebas de integración.
/// Reemplaza PostgreSQL por una base de datos en memoria y usa secretos de prueba.
/// </summary>
public class FabricaApiPruebas : WebApplicationFactory<Program>
{
    public const string PasswordDemo = "Prueba.Demo.2026";

    private readonly string _nombreBase = $"pruebas-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Host=no-se-usa-en-pruebas");
        builder.UseSetting("Jwt:Clave", "clave-de-pruebas-de-al-menos-32-caracteres!");
        builder.UseSetting("Seed:UsuariosDemo", "true");
        builder.UseSetting("Seed:PasswordDemo", PasswordDemo);
        builder.UseSetting("Archivos:Ruta", Path.Combine(Path.GetTempPath(), "tali-pruebas-archivos"));

        builder.ConfigureServices(services =>
        {
            var registrosDb = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                .ToList();
            foreach (var registro in registrosDb)
            {
                services.Remove(registro);
            }

            services.AddDbContext<AppDbContext>((sp, o) => o
                .UseInMemoryDatabase(_nombreBase)
                .AddInterceptors(sp.GetRequiredService<InterceptorBitacora>()));
        });
    }
}

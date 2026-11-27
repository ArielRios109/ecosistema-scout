using EcosistemaScout.Api.Configuracion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace EcosistemaScout.Tests;

public class ValidacionProduccionTests
{
    private const string ClaveDesarrollo = "clave-solo-para-desarrollo-local-no-usar-en-produccion-2026";
    private const string ClaveSegura = "una-clave-larga-y-aleatoria-de-produccion-0123456789";

    [Fact]
    public void Produccion_ConValoresDeDesarrollo_NoPermiteArrancar()
    {
        var configuracion = Configuracion(ClaveDesarrollo, "Host=db;Password=scout_desarrollo", usuariosDemo: true);

        var error = Assert.Throws<InvalidOperationException>(
            () => ValidacionProduccion.Verificar(configuracion, Entorno(Environments.Production)));

        Assert.Contains("Jwt:Clave", error.Message);
        Assert.Contains("ConnectionStrings:Default", error.Message);
        Assert.Contains("Seed:UsuariosDemo", error.Message);
    }

    [Fact]
    public void Produccion_ConValoresPropios_PermiteArrancar()
    {
        var configuracion = Configuracion(ClaveSegura, "Host=db;Password=Xk82-segura", usuariosDemo: false);

        ValidacionProduccion.Verificar(configuracion, Entorno(Environments.Production));
    }

    [Fact]
    public void Desarrollo_ConValoresDeDesarrollo_PermiteArrancar()
    {
        var configuracion = Configuracion(ClaveDesarrollo, "Host=localhost;Password=scout_desarrollo", usuariosDemo: true);

        ValidacionProduccion.Verificar(configuracion, Entorno(Environments.Development));
    }

    private static IConfiguration Configuracion(string claveJwt, string conexion, bool usuariosDemo) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Clave"] = claveJwt,
            ["ConnectionStrings:Default"] = conexion,
            ["Seed:UsuariosDemo"] = usuariosDemo.ToString(),
        }).Build();

    private static IHostEnvironment Entorno(string nombre) => new EntornoPrueba { EnvironmentName = nombre };

    private sealed class EntornoPrueba : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "EcosistemaScout.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

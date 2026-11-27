namespace EcosistemaScout.Api.Configuracion;

/// <summary>
/// Impide que la API arranque en producción con los valores de desarrollo que vienen en el repositorio
/// (clave JWT de ejemplo, contraseña de base de datos de desarrollo o usuarios de demostración).
/// </summary>
public static class ValidacionProduccion
{
    /// <summary>Texto que identifica a todos los valores de desarrollo incluidos en el repositorio.</summary>
    public const string MarcaDesarrollo = "desarrollo";

    public static void Verificar(IConfiguration configuracion, IHostEnvironment entorno)
    {
        if (!entorno.IsProduction())
        {
            return;
        }

        var problemas = new List<string>();

        var claveJwt = configuracion["Jwt:Clave"] ?? "";
        if (claveJwt.Contains(MarcaDesarrollo, StringComparison.OrdinalIgnoreCase))
        {
            problemas.Add("Jwt:Clave usa la clave de desarrollo.");
        }

        var conexion = configuracion.GetConnectionString("Default") ?? "";
        if (conexion.Contains(MarcaDesarrollo, StringComparison.OrdinalIgnoreCase))
        {
            problemas.Add("ConnectionStrings:Default usa la contraseña de desarrollo.");
        }

        if (configuracion.GetValue("Seed:UsuariosDemo", false))
        {
            problemas.Add("Seed:UsuariosDemo está activado (los usuarios de demostración son solo para desarrollo).");
        }

        if (problemas.Count > 0)
        {
            throw new InvalidOperationException(
                "La configuración de producción no es segura: " + string.Join(" ", problemas) +
                " Define valores propios en el archivo .env o en variables de entorno (ver README, sección Producción).");
        }
    }
}

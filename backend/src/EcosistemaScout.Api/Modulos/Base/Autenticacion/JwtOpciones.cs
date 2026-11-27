namespace EcosistemaScout.Api.Modulos.Base.Autenticacion;

/// <summary>Configuración de los tokens JWT (sección "Jwt" de la configuración).</summary>
public class JwtOpciones
{
    public const string Seccion = "Jwt";

    public string Emisor { get; set; } = "EcosistemaScout";
    public string Audiencia { get; set; } = "EcosistemaScout.Web";
    /// <summary>Clave secreta de firma (mínimo 32 caracteres). Nunca se guarda en el repositorio.</summary>
    public string Clave { get; set; } = "";
    public int ExpiracionMinutos { get; set; } = 480;
}

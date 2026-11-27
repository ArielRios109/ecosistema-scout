namespace EcosistemaScout.Api.Modulos.Base.Autorizacion;

/// <summary>Respuesta provisoria de los módulos que aún no se implementan.</summary>
public record EstadoModulo(string Modulo, string Estado)
{
    public static EstadoModulo EnConstruccion(string modulo) => new(modulo, "en construcción");
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcosistemaScout.Api.Datos;

/// <summary>Ayudas para configurar columnas comunes del modelo.</summary>
internal static class ConfiguracionComun
{
    /// <summary>Columnas created_at / updated_at con valor por defecto now(), como en schema.sql.</summary>
    public static void ConFechas<T>(this EntityTypeBuilder<T> e) where T : class, IConFechas
    {
        e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
    }

    /// <summary>Anotación que marca una columna con datos sensibles: la bitácora no guarda su valor.</summary>
    public const string AnotacionSensible = "Talí:Sensible";

    /// <summary>Marca la propiedad como sensible (por ejemplo, datos de salud cifrados).</summary>
    public static PropertyBuilder<TPropiedad> Sensible<TPropiedad>(this PropertyBuilder<TPropiedad> propiedad)
        => propiedad.HasAnnotation(AnotacionSensible, true);

    /// <summary>Restricción CHECK que limita una columna de texto a una lista de valores.</summary>
    public static string EnLista(string columna, params string[] valores)
        => $"{columna} IN ({string.Join(", ", valores.Select(v => $"'{v}'"))})";
}

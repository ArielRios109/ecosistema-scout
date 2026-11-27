namespace EcosistemaScout.Api.Datos;

/// <summary>
/// Entidades con fechas de creación y actualización (columnas created_at / updated_at).
/// El DbContext completa estos valores automáticamente al guardar.
/// </summary>
public interface IConFechas
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

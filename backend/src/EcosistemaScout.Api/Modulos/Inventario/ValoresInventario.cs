namespace EcosistemaScout.Api.Modulos.Inventario;

/// <summary>Valores permitidos en Inventario (los mismos que las restricciones CHECK de la base de datos).</summary>
public static class ValoresInventario
{
    public static readonly IReadOnlyList<string> Origenes = ["propio", "prestamo", "consignacion", "comodato", "donacion"];
    public static readonly IReadOnlyList<string> Estados = ["bueno", "regular", "malo", "reparacion"];
    public static readonly IReadOnlyList<string> MotivosBaja = ["venta", "robo", "donacion", "deterioro", "perdida", "otro"];

    /// <summary>Cómo vuelve un préstamo. "perdido" deja registro de que esa cantidad no regresó.</summary>
    public static readonly IReadOnlyList<string> EstadosDevolucion = ["bueno", "regular", "malo", "perdido"];

    /// <summary>Formatos de foto aceptados y su extensión.</summary>
    public static readonly IReadOnlyDictionary<string, string> TiposFoto = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public const long TamanoMaximoFoto = 3 * 1024 * 1024;
}

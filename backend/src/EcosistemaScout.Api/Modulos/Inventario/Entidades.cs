using EcosistemaScout.Api.Datos;

namespace EcosistemaScout.Api.Modulos.Inventario;

/// <summary>Categorías de bienes (carpas, cocina, herramientas, etc.).</summary>
public class CategoriaBien
{
    public long Id { get; set; }
    public string Nombre { get; set; } = null!;

    public ICollection<Bien> Bienes { get; set; } = new List<Bien>();
}

/// <summary>Bienes del grupo con los campos del formulario DAF-FOR-04.</summary>
public class Bien : IConFechas
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public long CategoriaId { get; set; }
    public int Cantidad { get; set; } = 1;
    public string? Marca { get; set; }
    public string? NumeroSerie { get; set; }
    public string? Descripcion { get; set; }
    public DateOnly? FechaAdquisicion { get; set; }
    public int? PrecioCompra { get; set; }
    public int? ValorEstimado { get; set; }
    /// <summary>propio, prestamo, consignacion, comodato o donacion.</summary>
    public string Origen { get; set; } = null!;
    /// <summary>bueno, regular, malo o reparacion.</summary>
    public string Estado { get; set; } = null!;
    public string? Ubicacion { get; set; }
    public long? ResponsableId { get; set; }
    public string? FotoPath { get; set; }
    public bool DadoDeBaja { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public CategoriaBien Categoria { get; set; } = null!;
    public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
    public BajaBien? Baja { get; set; }
}

/// <summary>Salidas de bienes a personas o actividades, con devolución.</summary>
public class Prestamo : IConFechas
{
    public long Id { get; set; }
    public long BienId { get; set; }
    public int Cantidad { get; set; }
    public string Destinatario { get; set; } = null!;
    public string? Actividad { get; set; }
    public DateOnly FechaSalida { get; set; }
    public DateOnly FechaDevolucionEsperada { get; set; }
    public DateOnly? FechaDevolucion { get; set; }
    public string? EstadoDevolucion { get; set; }
    public long RegistradoPor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Bien Bien { get; set; } = null!;
}

/// <summary>Bajas de inventario (DAF-FOR-09); el bien no se borra.</summary>
public class BajaBien
{
    public long Id { get; set; }
    public long BienId { get; set; }
    /// <summary>venta, robo, donacion, deterioro, perdida u otro.</summary>
    public string Motivo { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public string? Detalle { get; set; }
    public long RegistradoPor { get; set; }

    public Bien Bien { get; set; } = null!;
}

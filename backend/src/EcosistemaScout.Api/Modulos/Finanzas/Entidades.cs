using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Miembros;

namespace EcosistemaScout.Api.Modulos.Finanzas;

/// <summary>Plan de cuentas basado en el Presupuesto Grupal DAF-FOR-07.</summary>
public class ItemPresupuestario
{
    public long Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    /// <summary>ingreso o egreso.</summary>
    public string Tipo { get; set; } = null!;
    public bool Activo { get; set; } = true;
}

/// <summary>Separación del dinero por grupo, unidad o actividad.</summary>
public class CentroCosto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = null!;
    /// <summary>grupo, unidad o actividad.</summary>
    public string Tipo { get; set; } = null!;
    public long? UnidadId { get; set; }
    public bool Activo { get; set; } = true;

    public Unidad? Unidad { get; set; }
}

/// <summary>Ingresos y egresos de dinero; nunca se borran, solo se anulan.</summary>
public class Movimiento : IConFechas
{
    public long Id { get; set; }
    public int NumeroComprobante { get; set; }
    public short Anio { get; set; }
    /// <summary>ingreso o egreso.</summary>
    public string Tipo { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public long ItemId { get; set; }
    public long CentroCostoId { get; set; }
    public int Monto { get; set; }
    /// <summary>efectivo, transferencia u otro.</summary>
    public string MedioPago { get; set; } = null!;
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string Descripcion { get; set; } = null!;
    public string? RespaldoPath { get; set; }
    /// <summary>vigente o anulado.</summary>
    public string Estado { get; set; } = "vigente";
    public string? MotivoAnulacion { get; set; }
    public long? AnuladoPor { get; set; }
    public DateTime? AnuladoEn { get; set; }
    public long RegistradoPor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ItemPresupuestario Item { get; set; } = null!;
    public CentroCosto CentroCosto { get; set; } = null!;
}

/// <summary>Meses cerrados: sus movimientos ya no se editan.</summary>
public class CierreMensual
{
    public long Id { get; set; }
    public short Anio { get; set; }
    public short Mes { get; set; }
    public long SaldoInicial { get; set; }
    public long TotalIngresos { get; set; }
    public long TotalEgresos { get; set; }
    public long SaldoFinal { get; set; }
    public long CerradoPor { get; set; }
    public DateTime CerradoEn { get; set; }
}

/// <summary>Tipos de cobro: cuota mensual, registro anual, campamento, actividad.</summary>
public class ConceptoCobro
{
    public long Id { get; set; }
    public string Nombre { get; set; } = null!;
    /// <summary>mensual, anual, campamento, actividad, venta u otro.</summary>
    public string Tipo { get; set; } = null!;
    public long ItemId { get; set; }
    public int? MontoSugerido { get; set; }

    public ItemPresupuestario Item { get; set; } = null!;
}

/// <summary>Deuda de un beneficiario por un concepto, con vencimiento.</summary>
public class Cargo : IConFechas
{
    public long Id { get; set; }
    public long BeneficiarioId { get; set; }
    public long ConceptoId { get; set; }
    public long CentroCostoId { get; set; }
    public string Descripcion { get; set; } = null!;
    public int Monto { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    /// <summary>pendiente, parcial, pagado o anulado.</summary>
    public string Estado { get; set; } = "pendiente";
    public long CreadoPor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Beneficiario Beneficiario { get; set; } = null!;
    public ConceptoCobro Concepto { get; set; } = null!;
    public CentroCosto CentroCosto { get; set; } = null!;
    public ICollection<Abono> Abonos { get; set; } = new List<Abono>();
}

/// <summary>Aplicación de un ingreso a uno o más cargos.</summary>
public class Abono
{
    public long Id { get; set; }
    public long CargoId { get; set; }
    public long MovimientoId { get; set; }
    public int Monto { get; set; }
    public DateTime CreatedAt { get; set; }

    public Cargo Cargo { get; set; } = null!;
    public Movimiento Movimiento { get; set; } = null!;
}

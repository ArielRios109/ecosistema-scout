using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Finanzas;
using EcosistemaScout.Api.Modulos.Miembros;

namespace EcosistemaScout.Api.Modulos.Portal;

/// <summary>Transferencias informadas por apoderados, pendientes de revisión.</summary>
public class PagoInformado : IConFechas
{
    public long Id { get; set; }
    public long ApoderadoId { get; set; }
    public DateOnly FechaTransferencia { get; set; }
    public int Monto { get; set; }
    public string BancoOrigen { get; set; } = null!;
    public string NumeroOperacion { get; set; } = null!;
    public string ComprobantePath { get; set; } = null!;
    /// <summary>pendiente, aprobado o rechazado.</summary>
    public string Estado { get; set; } = "pendiente";
    public string? MotivoRechazo { get; set; }
    public long? RevisadoPor { get; set; }
    public DateTime? RevisadoEn { get; set; }
    public long? MovimientoId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Apoderado Apoderado { get; set; } = null!;
    public Movimiento? Movimiento { get; set; }
    public ICollection<PagoInformadoCargo> Cargos { get; set; } = new List<PagoInformadoCargo>();
}

/// <summary>Cargos que el apoderado declara pagar con una transferencia.</summary>
public class PagoInformadoCargo
{
    public long PagoInformadoId { get; set; }
    public long CargoId { get; set; }
    public int MontoAplicado { get; set; }

    public PagoInformado PagoInformado { get; set; } = null!;
    public Cargo Cargo { get; set; } = null!;
}

using EcosistemaScout.Api.Datos;

namespace EcosistemaScout.Api.Modulos.Miembros;

/// <summary>Unidades del grupo y su rama.</summary>
public class Unidad : IConFechas
{
    public long Id { get; set; }
    public string Nombre { get; set; } = null!;
    /// <summary>golondrinas, lobatos, guias, scouts, pioneros o caminantes.</summary>
    public string Rama { get; set; } = null!;
    public bool Activa { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Patrulla> Patrullas { get; set; } = new List<Patrulla>();
    public ICollection<Beneficiario> Beneficiarios { get; set; } = new List<Beneficiario>();
}

/// <summary>Subdivisiones de una unidad.</summary>
public class Patrulla
{
    public long Id { get; set; }
    public long UnidadId { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activa { get; set; } = true;

    public Unidad Unidad { get; set; } = null!;
}

/// <summary>Niños, niñas y jóvenes del grupo.</summary>
public class Beneficiario : IConFechas
{
    public long Id { get; set; }
    public string Rut { get; set; } = null!;
    public string Nombres { get; set; } = null!;
    public string Apellidos { get; set; } = null!;
    public DateOnly FechaNacimiento { get; set; }
    public string? Direccion { get; set; }
    public long UnidadId { get; set; }
    public long? PatrullaId { get; set; }
    public DateOnly FechaIngreso { get; set; }
    /// <summary>activo o retirado.</summary>
    public string Estado { get; set; } = "activo";
    public short? RegistroInstitucionalAnio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Unidad Unidad { get; set; } = null!;
    public Patrulla? Patrulla { get; set; }
    public FichaMedica? FichaMedica { get; set; }
    public ICollection<ContactoEmergencia> ContactosEmergencia { get; set; } = new List<ContactoEmergencia>();
    public ICollection<BeneficiarioApoderado> Apoderados { get; set; } = new List<BeneficiarioApoderado>();
}

/// <summary>Ficha médica y de necesidades emocionales; los campos sensibles se guardarán cifrados.</summary>
public class FichaMedica : IConFechas
{
    public long Id { get; set; }
    public long BeneficiarioId { get; set; }
    public string? Prevision { get; set; }
    public string? GrupoSanguineo { get; set; }
    public string? Alergias { get; set; }
    public string? Enfermedades { get; set; }
    public string? Medicamentos { get; set; }
    public string? RestriccionesAlimentarias { get; set; }
    public string? NecesidadesEmocionales { get; set; }
    public long? ActualizadoPor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Beneficiario Beneficiario { get; set; } = null!;
}

/// <summary>Contactos de emergencia de un beneficiario.</summary>
public class ContactoEmergencia
{
    public long Id { get; set; }
    public long BeneficiarioId { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Parentesco { get; set; }
    public string Telefono { get; set; } = null!;

    public Beneficiario Beneficiario { get; set; } = null!;
}

/// <summary>Adultos responsables de uno o más beneficiarios.</summary>
public class Apoderado : IConFechas
{
    public long Id { get; set; }
    public long? UsuarioId { get; set; }
    public string Rut { get; set; } = null!;
    public string Nombres { get; set; } = null!;
    public string Apellidos { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Telefono { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<BeneficiarioApoderado> Beneficiarios { get; set; } = new List<BeneficiarioApoderado>();
}

/// <summary>Relación N:M entre beneficiarios y apoderados.</summary>
public class BeneficiarioApoderado
{
    public long BeneficiarioId { get; set; }
    public long ApoderadoId { get; set; }
    public string Parentesco { get; set; } = null!;
    public bool EsPrincipal { get; set; }

    public Beneficiario Beneficiario { get; set; } = null!;
    public Apoderado Apoderado { get; set; } = null!;
}

/// <summary>Dirigentes, guiadoras y otros adultos con cargo.</summary>
public class Voluntario : IConFechas
{
    public long Id { get; set; }
    public long? UsuarioId { get; set; }
    public string Rut { get; set; } = null!;
    public string Nombres { get; set; } = null!;
    public string Apellidos { get; set; } = null!;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CargoVoluntario> Cargos { get; set; } = new List<CargoVoluntario>();
}

/// <summary>Cargos que ocupa un voluntario, con período.</summary>
public class CargoVoluntario
{
    public long Id { get; set; }
    public long VoluntarioId { get; set; }
    public long? UnidadId { get; set; }
    public string Cargo { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaTermino { get; set; }

    public Voluntario Voluntario { get; set; } = null!;
    public Unidad? Unidad { get; set; }
}

/// <summary>Ingresos, cambios de unidad o patrulla y retiros.</summary>
public class HistorialBeneficiario
{
    public long Id { get; set; }
    public long BeneficiarioId { get; set; }
    /// <summary>ingreso, cambio_unidad, cambio_patrulla, retiro o reingreso.</summary>
    public string Tipo { get; set; } = null!;
    public long? UnidadOrigenId { get; set; }
    public long? UnidadDestinoId { get; set; }
    public DateOnly Fecha { get; set; }
    public string? Detalle { get; set; }
    public long? UsuarioId { get; set; }

    public Beneficiario Beneficiario { get; set; } = null!;
}

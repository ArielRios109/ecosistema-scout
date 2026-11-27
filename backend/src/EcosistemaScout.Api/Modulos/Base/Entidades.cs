using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Miembros;
using Microsoft.AspNetCore.Identity;

namespace EcosistemaScout.Api.Modulos.Base;

/// <summary>Datos del grupo dueño de la instancia (una sola fila).</summary>
public class ConfiguracionGrupo : IConFechas
{
    public long Id { get; set; }
    public string NombreGrupo { get; set; } = null!;
    public string? NumeroGrupo { get; set; }
    public string? Distrito { get; set; }
    public string? Zona { get; set; }
    public string? Banco { get; set; }
    public string? TipoCuenta { get; set; }
    public string? NumeroCuenta { get; set; }
    public string? TitularCuenta { get; set; }
    public string? RutTitular { get; set; }
    public string? EmailTesoreria { get; set; }
    public string? LogoPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Personas que ingresan al sistema (tabla usuarios, gestionada con ASP.NET Identity).</summary>
public class Usuario : IdentityUser<long>, IConFechas
{
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public DateTime? UltimoAcceso { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UsuarioRol> Roles { get; set; } = new List<UsuarioRol>();
}

/// <summary>Roles del sistema: administrador, tesoreria, dirigente, apoderado, revisor.</summary>
public class Rol : IdentityRole<long>
{
    public string? Descripcion { get; set; }

    public ICollection<UsuarioRol> Usuarios { get; set; } = new List<UsuarioRol>();
}

/// <summary>Asignación de roles (acumulables) con alcance opcional a una unidad.</summary>
public class UsuarioRol : IdentityUserRole<long>
{
    public long? UnidadId { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
    public Unidad? Unidad { get; set; }
}

/// <summary>Registro de auditoría de toda creación, modificación, aprobación y anulación.</summary>
public class Bitacora
{
    public long Id { get; set; }
    public long? UsuarioId { get; set; }
    public string Accion { get; set; } = null!;
    public string Entidad { get; set; } = null!;
    public long? EntidadId { get; set; }
    /// <summary>Estado anterior en formato JSON (columna jsonb).</summary>
    public string? Antes { get; set; }
    /// <summary>Estado posterior en formato JSON (columna jsonb).</summary>
    public string? Despues { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Nombres de los roles del sistema.</summary>
public static class Roles
{
    public const string Administrador = "administrador";
    public const string Tesoreria = "tesoreria";
    public const string Dirigente = "dirigente";
    public const string Apoderado = "apoderado";
    public const string Revisor = "revisor";
}

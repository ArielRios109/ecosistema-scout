using Microsoft.AspNetCore.Authorization;

namespace EcosistemaScout.Api.Modulos.Base.Autorizacion;

/// <summary>
/// Políticas de autorización por módulo. Definen qué roles pueden usar cada módulo.
/// El frontend replica esta misma tabla (core/auth/permisos.ts) solo para mostrar u ocultar menús;
/// la seguridad real está aquí, en la API.
/// </summary>
public static class Politicas
{
    // Lectura (ver el módulo)
    public const string Miembros = "Miembros";
    public const string Inventario = "Inventario";
    public const string Finanzas = "Finanzas";
    public const string Portal = "Portal";
    public const string Administracion = "Administracion";

    // Escritura (crear, modificar, anular). El revisor solo consulta.
    /// <summary>Registrar préstamos y devoluciones (también el dirigente).</summary>
    public const string InventarioEscritura = "InventarioEscritura";
    /// <summary>Administrar bienes, categorías y bajas.</summary>
    public const string InventarioGestion = "InventarioGestion";
    public const string FinanzasEscritura = "FinanzasEscritura";

    private static readonly Dictionary<string, string[]> RolesPorPolitica = new()
    {
        [Miembros] = [Roles.Administrador, Roles.Dirigente],
        [Inventario] = [Roles.Administrador, Roles.Tesoreria, Roles.Dirigente, Roles.Revisor],
        [Finanzas] = [Roles.Administrador, Roles.Tesoreria, Roles.Revisor],
        [Portal] = [Roles.Administrador, Roles.Tesoreria, Roles.Apoderado],
        [Administracion] = [Roles.Administrador],
        [InventarioEscritura] = [Roles.Administrador, Roles.Tesoreria, Roles.Dirigente],
        [InventarioGestion] = [Roles.Administrador, Roles.Tesoreria],
        [FinanzasEscritura] = [Roles.Administrador, Roles.Tesoreria],
    };

    /// <summary>Registra todas las políticas en la configuración de autorización.</summary>
    public static void Registrar(AuthorizationOptions opciones)
    {
        foreach (var (politica, roles) in RolesPorPolitica)
        {
            opciones.AddPolicy(politica, p => p.RequireAuthenticatedUser().RequireRole(roles));
        }
    }
}

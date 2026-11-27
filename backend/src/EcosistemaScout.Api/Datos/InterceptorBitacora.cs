using System.Text.Encodings.Web;
using System.Text.Json;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Modulos.Base;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EcosistemaScout.Api.Datos;

/// <summary>
/// Registra en la tabla bitacora toda creación, modificación y eliminación de datos:
/// quién la hizo, desde qué IP, sobre qué registro y los valores antes y después.
/// Nunca guarda contraseñas, claves ni contenido cifrado.
/// </summary>
public class InterceptorBitacora(IUsuarioActual usuarioActual, TimeProvider reloj) : SaveChangesInterceptor
{
    /// <summary>Propiedades que no se registran (secretas, técnicas o que cambian en cada inicio de sesión).</summary>
    private static readonly HashSet<string> PropiedadesExcluidas =
    [
        nameof(Usuario.PasswordHash), nameof(Usuario.SecurityStamp), nameof(Usuario.ConcurrencyStamp),
        nameof(Usuario.UltimoAcceso), nameof(Usuario.AccessFailedCount), nameof(Usuario.LockoutEnd),
        nameof(IConFechas.CreatedAt), nameof(IConFechas.UpdatedAt),
        nameof(Rol.ConcurrencyStamp),
    ];

    /// <summary>
    /// Tipos de entidad que no se auditan: la propia bitácora, tablas internas de Identity y las llaves de cifrado
    /// (registrarlas en la bitácora expondría las llaves).
    /// </summary>
    private static readonly HashSet<Type> TiposExcluidos =
    [
        typeof(Bitacora), typeof(IdentityUserToken<long>), typeof(IdentityUserLogin<long>), typeof(DataProtectionKey),
    ];

    /// <summary>JSON legible (tildes y ñ sin escapar) para revisar la bitácora directamente en la base de datos.</summary>
    private static readonly JsonSerializerOptions OpcionesJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly List<(EntityEntry Entrada, string Accion, Dictionary<string, object?>? Antes)> _pendientes = [];
    private bool _guardandoBitacora;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capturar(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capturar(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (Registrar(eventData.Context))
        {
            _guardandoBitacora = true;
            try { eventData.Context!.SaveChanges(); }
            finally { _guardandoBitacora = false; }
        }
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (Registrar(eventData.Context))
        {
            _guardandoBitacora = true;
            try { await eventData.Context!.SaveChangesAsync(cancellationToken); }
            finally { _guardandoBitacora = false; }
        }
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pendientes.Clear();

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendientes.Clear();
        return Task.CompletedTask;
    }

    /// <summary>Antes de guardar: anota qué cambió y los valores anteriores.</summary>
    private void Capturar(DbContext? contexto)
    {
        if (contexto is null || _guardandoBitacora) return;
        _pendientes.Clear();

        foreach (var entrada in contexto.ChangeTracker.Entries())
        {
            if (TiposExcluidos.Contains(entrada.Metadata.ClrType)) continue;

            switch (entrada.State)
            {
                case EntityState.Added:
                    _pendientes.Add((entrada, "crear", null));
                    break;
                case EntityState.Deleted:
                    _pendientes.Add((entrada, "eliminar", Valores(entrada, original: true, soloModificadas: false)));
                    break;
                case EntityState.Modified:
                    var antes = Valores(entrada, original: true, soloModificadas: true);
                    if (antes.Count > 0) _pendientes.Add((entrada, "modificar", antes));
                    break;
            }
        }
    }

    /// <summary>Después de guardar (ya con los Id asignados): agrega las filas de bitácora. Devuelve true si hay algo que guardar.</summary>
    private bool Registrar(DbContext? contexto)
    {
        if (contexto is null || _guardandoBitacora || _pendientes.Count == 0) return false;

        var ahora = reloj.GetUtcNow().UtcDateTime;
        foreach (var (entrada, accion, antes) in _pendientes)
        {
            // En una modificación, el "después" incluye solo las mismas columnas que cambiaron (las del "antes").
            var despues = accion switch
            {
                "eliminar" => null,
                "modificar" => Valores(entrada, original: false, soloModificadas: false)
                    .Where(par => antes!.ContainsKey(par.Key))
                    .ToDictionary(par => par.Key, par => par.Value),
                _ => Valores(entrada, original: false, soloModificadas: false),
            };
            contexto.Add(new Bitacora
            {
                UsuarioId = usuarioActual.Id,
                Accion = accion,
                Entidad = entrada.Metadata.GetTableName() ?? entrada.Metadata.ClrType.Name,
                EntidadId = IdDe(entrada),
                Antes = antes is null ? null : JsonSerializer.Serialize(antes, OpcionesJson),
                Despues = despues is null ? null : JsonSerializer.Serialize(despues, OpcionesJson),
                Ip = usuarioActual.Ip,
                CreatedAt = ahora,
            });
        }
        _pendientes.Clear();
        return true;
    }

    private static Dictionary<string, object?> Valores(EntityEntry entrada, bool original, bool soloModificadas)
    {
        var valores = new Dictionary<string, object?>();
        foreach (var propiedad in entrada.Properties)
        {
            var nombre = propiedad.Metadata.Name;
            if (PropiedadesExcluidas.Contains(nombre)) continue;
            if (soloModificadas && !propiedad.IsModified) continue;
            if (propiedad.Metadata.FindAnnotation(ConfiguracionComun.AnotacionSensible) is not null)
            {
                valores[nombre] = "(dato protegido)";
                continue;
            }
            valores[nombre] = original ? propiedad.OriginalValue : propiedad.CurrentValue;
        }
        return valores;
    }

    private static long? IdDe(EntityEntry entrada)
    {
        var clave = entrada.Metadata.FindPrimaryKey();
        if (clave is null || clave.Properties.Count != 1) return null;
        return entrada.Property(clave.Properties[0].Name).CurrentValue switch
        {
            long l => l,
            int i => i,
            _ => null,
        };
    }
}

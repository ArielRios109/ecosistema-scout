using EcosistemaScout.Api.Modulos.Base;
using EcosistemaScout.Api.Modulos.Finanzas;
using EcosistemaScout.Api.Modulos.Inventario;
using EcosistemaScout.Api.Modulos.Miembros;
using EcosistemaScout.Api.Modulos.Portal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EcosistemaScout.Api.Datos;

/// <summary>
/// Contexto de datos de la aplicación. Mapea las 28 tablas del modelo de datos
/// (database/schema.sql) e integra ASP.NET Identity sobre las tablas usuarios, roles y usuario_rol.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Usuario, Rol, long, IdentityUserClaim<long>, UsuarioRol,
        IdentityUserLogin<long>, IdentityRoleClaim<long>, IdentityUserToken<long>>(options)
{
    // Módulo Base (usuarios, roles y usuario_rol vienen de IdentityDbContext)
    public DbSet<ConfiguracionGrupo> ConfiguracionGrupo => Set<ConfiguracionGrupo>();
    public DbSet<Bitacora> Bitacora => Set<Bitacora>();

    // Módulo Miembros
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<Patrulla> Patrullas => Set<Patrulla>();
    public DbSet<Beneficiario> Beneficiarios => Set<Beneficiario>();
    public DbSet<FichaMedica> FichasMedicas => Set<FichaMedica>();
    public DbSet<ContactoEmergencia> ContactosEmergencia => Set<ContactoEmergencia>();
    public DbSet<Apoderado> Apoderados => Set<Apoderado>();
    public DbSet<BeneficiarioApoderado> BeneficiarioApoderado => Set<BeneficiarioApoderado>();
    public DbSet<Voluntario> Voluntarios => Set<Voluntario>();
    public DbSet<CargoVoluntario> CargosVoluntario => Set<CargoVoluntario>();
    public DbSet<HistorialBeneficiario> HistorialBeneficiario => Set<HistorialBeneficiario>();

    // Módulo Inventario
    public DbSet<CategoriaBien> CategoriasBien => Set<CategoriaBien>();
    public DbSet<Bien> Bienes => Set<Bien>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();
    public DbSet<BajaBien> BajasBien => Set<BajaBien>();

    // Módulo Financiamiento
    public DbSet<ItemPresupuestario> ItemsPresupuestarios => Set<ItemPresupuestario>();
    public DbSet<CentroCosto> CentrosCosto => Set<CentroCosto>();
    public DbSet<Movimiento> Movimientos => Set<Movimiento>();
    public DbSet<CierreMensual> CierresMensuales => Set<CierreMensual>();
    public DbSet<ConceptoCobro> ConceptosCobro => Set<ConceptoCobro>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Abono> Abonos => Set<Abono>();

    // Módulo Portal de pagos
    public DbSet<PagoInformado> PagosInformados => Set<PagoInformado>();
    public DbSet<PagoInformadoCargo> PagoInformadoCargo => Set<PagoInformadoCargo>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Igual que schema.sql: las claves foráneas NO borran en cascada salvo que se indique explícitamente.
        configurationBuilder.Conventions.Remove<CascadeDeleteConvention>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ModeloBase.Configurar(builder);
        ModeloMiembros.Configurar(builder);
        ModeloInventario.Configurar(builder);
        ModeloFinanzas.Configurar(builder);
        ModeloPortal.Configurar(builder);

        // Índices de apoyo adicionales definidos en schema.sql
        builder.Entity<Movimiento>().HasIndex(m => m.Fecha);
        builder.Entity<Cargo>().HasIndex(c => c.Estado);
        builder.Entity<PagoInformado>().HasIndex(p => p.Estado);
        builder.Entity<Bitacora>().HasIndex(b => new { b.Entidad, b.EntidadId });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        CompletarFechas();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        CompletarFechas();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void CompletarFechas()
    {
        var ahora = DateTime.UtcNow;
        foreach (var entrada in ChangeTracker.Entries<IConFechas>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.CreatedAt = ahora;
                entrada.Entity.UpdatedAt = ahora;
            }
            else if (entrada.State == EntityState.Modified)
            {
                entrada.Entity.UpdatedAt = ahora;
            }
        }
    }
}

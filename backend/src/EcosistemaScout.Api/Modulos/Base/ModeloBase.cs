using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Miembros;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Base;

/// <summary>Mapeo de las tablas del módulo Base.</summary>
internal static class ModeloBase
{
    public static void Configurar(ModelBuilder builder)
    {
        builder.Entity<ConfiguracionGrupo>(e =>
        {
            e.ToTable("configuracion_grupo");
            e.Property(x => x.NombreGrupo).HasMaxLength(120);
            e.Property(x => x.NumeroGrupo).HasMaxLength(20);
            e.Property(x => x.Distrito).HasMaxLength(80);
            e.Property(x => x.Zona).HasMaxLength(80);
            e.Property(x => x.Banco).HasMaxLength(60);
            e.Property(x => x.TipoCuenta).HasMaxLength(30);
            e.Property(x => x.NumeroCuenta).HasMaxLength(30);
            e.Property(x => x.TitularCuenta).HasMaxLength(120);
            e.Property(x => x.RutTitular).HasMaxLength(12);
            e.Property(x => x.EmailTesoreria).HasMaxLength(120);
            e.Property(x => x.LogoPath).HasMaxLength(255);
            e.ConFechas();
        });

        // ----- ASP.NET Identity sobre las tablas del modelo -----
        builder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.ConFechas();
        });

        builder.Entity<Rol>(e =>
        {
            e.ToTable("roles");
            e.Property(x => x.Name).HasColumnName("nombre").HasMaxLength(40).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(200);
        });

        builder.Entity<UsuarioRol>(e =>
        {
            e.ToTable("usuario_rol");
            e.Property(x => x.UserId).HasColumnName("usuario_id");
            e.Property(x => x.RoleId).HasColumnName("rol_id");
            e.HasOne(x => x.Usuario).WithMany(u => u.Roles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Rol).WithMany(r => r.Usuarios).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Unidad).WithMany().HasForeignKey(x => x.UnidadId);
        });

        // Tablas auxiliares de Identity (claims, logins externos y tokens)
        builder.Entity<IdentityUserClaim<long>>(e =>
        {
            e.ToTable("usuario_claims");
            e.Property(x => x.UserId).HasColumnName("usuario_id");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<IdentityUserLogin<long>>(e =>
        {
            e.ToTable("usuario_logins");
            e.Property(x => x.UserId).HasColumnName("usuario_id");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<IdentityUserToken<long>>(e =>
        {
            e.ToTable("usuario_tokens");
            e.Property(x => x.UserId).HasColumnName("usuario_id");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<IdentityRoleClaim<long>>(e =>
        {
            e.ToTable("rol_claims");
            e.Property(x => x.RoleId).HasColumnName("rol_id");
            e.HasOne<Rol>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Bitacora>(e =>
        {
            e.ToTable("bitacora");
            e.Property(x => x.Accion).HasMaxLength(30);
            e.Property(x => x.Entidad).HasMaxLength(60);
            e.Property(x => x.Antes).HasColumnType("jsonb");
            e.Property(x => x.Despues).HasColumnType("jsonb");
            e.Property(x => x.Ip).HasMaxLength(45);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
        });
    }
}

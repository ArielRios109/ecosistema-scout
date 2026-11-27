using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base;
using EcosistemaScout.Api.Modulos.Miembros;
using Microsoft.EntityFrameworkCore;
using static EcosistemaScout.Api.Datos.ConfiguracionComun;

namespace EcosistemaScout.Api.Modulos.Inventario;

/// <summary>Mapeo de las tablas del módulo Inventario.</summary>
internal static class ModeloInventario
{
    public static void Configurar(ModelBuilder builder)
    {
        builder.Entity<CategoriaBien>(e =>
        {
            e.ToTable("categorias_bien");
            e.Property(x => x.Nombre).HasMaxLength(60);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        builder.Entity<Bien>(e =>
        {
            e.ToTable("bienes", t =>
            {
                t.HasCheckConstraint("ck_bienes_cantidad", "cantidad >= 0");
                t.HasCheckConstraint("ck_bienes_precio_compra", "precio_compra >= 0");
                t.HasCheckConstraint("ck_bienes_valor_estimado", "valor_estimado >= 0");
                t.HasCheckConstraint("ck_bienes_origen",
                    EnLista("origen", "propio", "prestamo", "consignacion", "comodato", "donacion"));
                t.HasCheckConstraint("ck_bienes_estado", EnLista("estado", "bueno", "regular", "malo", "reparacion"));
            });
            e.Property(x => x.Codigo).HasMaxLength(20);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Marca).HasMaxLength(60);
            e.Property(x => x.NumeroSerie).HasMaxLength(60);
            e.Property(x => x.Origen).HasMaxLength(15);
            e.Property(x => x.Estado).HasMaxLength(15);
            e.Property(x => x.Ubicacion).HasMaxLength(120);
            e.Property(x => x.FotoPath).HasMaxLength(255);
            e.HasOne(x => x.Categoria).WithMany(c => c.Bienes).HasForeignKey(x => x.CategoriaId);
            e.HasOne<Voluntario>().WithMany().HasForeignKey(x => x.ResponsableId);
            e.ConFechas();
        });

        builder.Entity<Prestamo>(e =>
        {
            e.ToTable("prestamos", t => t.HasCheckConstraint("ck_prestamos_cantidad", "cantidad > 0"));
            e.Property(x => x.Destinatario).HasMaxLength(120);
            e.Property(x => x.Actividad).HasMaxLength(120);
            e.Property(x => x.EstadoDevolucion).HasMaxLength(15);
            e.HasOne(x => x.Bien).WithMany(b => b.Prestamos).HasForeignKey(x => x.BienId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPor);
            e.ConFechas();
        });

        builder.Entity<BajaBien>(e =>
        {
            e.ToTable("bajas_bien", t => t.HasCheckConstraint("ck_bajas_bien_motivo",
                EnLista("motivo", "venta", "robo", "donacion", "deterioro", "perdida", "otro")));
            e.Property(x => x.Motivo).HasMaxLength(15);
            e.Property(x => x.Detalle).HasMaxLength(255);
            e.HasIndex(x => x.BienId).IsUnique();
            e.HasOne(x => x.Bien).WithOne(b => b.Baja).HasForeignKey<BajaBien>(x => x.BienId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPor);
        });
    }
}

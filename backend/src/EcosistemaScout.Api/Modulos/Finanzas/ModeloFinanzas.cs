using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base;
using Microsoft.EntityFrameworkCore;
using static EcosistemaScout.Api.Datos.ConfiguracionComun;

namespace EcosistemaScout.Api.Modulos.Finanzas;

/// <summary>Mapeo de las tablas del módulo Financiamiento.</summary>
internal static class ModeloFinanzas
{
    public static void Configurar(ModelBuilder builder)
    {
        builder.Entity<ItemPresupuestario>(e =>
        {
            e.ToTable("items_presupuestarios", t => t.HasCheckConstraint("ck_items_presupuestarios_tipo",
                EnLista("tipo", "ingreso", "egreso")));
            e.Property(x => x.Codigo).HasMaxLength(10);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Tipo).HasMaxLength(7);
        });

        builder.Entity<CentroCosto>(e =>
        {
            e.ToTable("centros_costo", t => t.HasCheckConstraint("ck_centros_costo_tipo",
                EnLista("tipo", "grupo", "unidad", "actividad")));
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Tipo).HasMaxLength(10);
            e.HasOne(x => x.Unidad).WithMany().HasForeignKey(x => x.UnidadId);
        });

        builder.Entity<Movimiento>(e =>
        {
            e.ToTable("movimientos", t =>
            {
                t.HasCheckConstraint("ck_movimientos_tipo", EnLista("tipo", "ingreso", "egreso"));
                t.HasCheckConstraint("ck_movimientos_monto", "monto > 0");
                t.HasCheckConstraint("ck_movimientos_medio_pago", EnLista("medio_pago", "efectivo", "transferencia", "otro"));
                t.HasCheckConstraint("ck_movimientos_estado", EnLista("estado", "vigente", "anulado"));
            });
            e.Property(x => x.Tipo).HasMaxLength(7);
            e.Property(x => x.MedioPago).HasMaxLength(15);
            e.Property(x => x.TipoDocumento).HasMaxLength(15);
            e.Property(x => x.NumeroDocumento).HasMaxLength(30);
            e.Property(x => x.Descripcion).HasMaxLength(255);
            e.Property(x => x.RespaldoPath).HasMaxLength(255);
            e.Property(x => x.Estado).HasMaxLength(8);
            e.Property(x => x.MotivoAnulacion).HasMaxLength(255);
            e.HasIndex(x => new { x.Tipo, x.Anio, x.NumeroComprobante }).IsUnique();
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId);
            e.HasOne(x => x.CentroCosto).WithMany().HasForeignKey(x => x.CentroCostoId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.AnuladoPor);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RegistradoPor);
            e.ConFechas();
        });

        builder.Entity<CierreMensual>(e =>
        {
            e.ToTable("cierres_mensuales", t => t.HasCheckConstraint("ck_cierres_mensuales_mes", "mes BETWEEN 1 AND 12"));
            e.HasIndex(x => new { x.Anio, x.Mes }).IsUnique();
            e.Property(x => x.CerradoEn).HasDefaultValueSql("now()");
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CerradoPor);
        });

        builder.Entity<ConceptoCobro>(e =>
        {
            e.ToTable("conceptos_cobro", t =>
            {
                t.HasCheckConstraint("ck_conceptos_cobro_tipo",
                    EnLista("tipo", "mensual", "anual", "campamento", "actividad", "venta", "otro"));
                t.HasCheckConstraint("ck_conceptos_cobro_monto_sugerido", "monto_sugerido >= 0");
            });
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Tipo).HasMaxLength(12);
            e.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId);
        });

        builder.Entity<Cargo>(e =>
        {
            e.ToTable("cargos", t =>
            {
                t.HasCheckConstraint("ck_cargos_monto", "monto > 0");
                t.HasCheckConstraint("ck_cargos_estado", EnLista("estado", "pendiente", "parcial", "pagado", "anulado"));
            });
            e.Property(x => x.Descripcion).HasMaxLength(160);
            e.Property(x => x.Estado).HasMaxLength(10);
            e.HasOne(x => x.Beneficiario).WithMany().HasForeignKey(x => x.BeneficiarioId);
            e.HasOne(x => x.Concepto).WithMany().HasForeignKey(x => x.ConceptoId);
            e.HasOne(x => x.CentroCosto).WithMany().HasForeignKey(x => x.CentroCostoId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CreadoPor);
            e.ConFechas();
        });

        builder.Entity<Abono>(e =>
        {
            e.ToTable("abonos", t => t.HasCheckConstraint("ck_abonos_monto", "monto > 0"));
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(x => x.Cargo).WithMany(c => c.Abonos).HasForeignKey(x => x.CargoId);
            e.HasOne(x => x.Movimiento).WithMany().HasForeignKey(x => x.MovimientoId);
        });
    }
}

using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base;
using Microsoft.EntityFrameworkCore;
using static EcosistemaScout.Api.Datos.ConfiguracionComun;

namespace EcosistemaScout.Api.Modulos.Portal;

/// <summary>Mapeo de las tablas del módulo Portal de pagos.</summary>
internal static class ModeloPortal
{
    public static void Configurar(ModelBuilder builder)
    {
        builder.Entity<PagoInformado>(e =>
        {
            e.ToTable("pagos_informados", t =>
            {
                t.HasCheckConstraint("ck_pagos_informados_monto", "monto > 0");
                t.HasCheckConstraint("ck_pagos_informados_estado", EnLista("estado", "pendiente", "aprobado", "rechazado"));
            });
            e.Property(x => x.BancoOrigen).HasMaxLength(60);
            e.Property(x => x.NumeroOperacion).HasMaxLength(40);
            e.Property(x => x.ComprobantePath).HasMaxLength(255);
            e.Property(x => x.Estado).HasMaxLength(10);
            e.Property(x => x.MotivoRechazo).HasMaxLength(255);
            e.HasIndex(x => new { x.BancoOrigen, x.NumeroOperacion }).IsUnique();
            e.HasIndex(x => x.MovimientoId).IsUnique();
            e.HasOne(x => x.Apoderado).WithMany().HasForeignKey(x => x.ApoderadoId);
            e.HasOne(x => x.Movimiento).WithMany().HasForeignKey(x => x.MovimientoId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.RevisadoPor);
            e.ConFechas();
        });

        builder.Entity<PagoInformadoCargo>(e =>
        {
            e.ToTable("pago_informado_cargo", t => t.HasCheckConstraint("ck_pago_informado_cargo_monto_aplicado", "monto_aplicado > 0"));
            e.HasKey(x => new { x.PagoInformadoId, x.CargoId });
            e.HasOne(x => x.PagoInformado).WithMany(p => p.Cargos)
                .HasForeignKey(x => x.PagoInformadoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Cargo).WithMany().HasForeignKey(x => x.CargoId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base;
using Microsoft.EntityFrameworkCore;
using static EcosistemaScout.Api.Datos.ConfiguracionComun;

namespace EcosistemaScout.Api.Modulos.Miembros;

/// <summary>Mapeo de las tablas del módulo Gestión de Miembros.</summary>
internal static class ModeloMiembros
{
    public static void Configurar(ModelBuilder builder)
    {
        builder.Entity<Unidad>(e =>
        {
            e.ToTable("unidades", t => t.HasCheckConstraint("ck_unidades_rama",
                EnLista("rama", "golondrinas", "lobatos", "guias", "scouts", "pioneros", "caminantes")));
            e.Property(x => x.Nombre).HasMaxLength(80);
            e.Property(x => x.Rama).HasMaxLength(30);
            e.ConFechas();
        });

        builder.Entity<Patrulla>(e =>
        {
            e.ToTable("patrullas");
            e.Property(x => x.Nombre).HasMaxLength(80);
            e.HasIndex(x => new { x.UnidadId, x.Nombre }).IsUnique();
            e.HasOne(x => x.Unidad).WithMany(u => u.Patrullas).HasForeignKey(x => x.UnidadId);
        });

        builder.Entity<Beneficiario>(e =>
        {
            e.ToTable("beneficiarios", t => t.HasCheckConstraint("ck_beneficiarios_estado",
                EnLista("estado", "activo", "retirado")));
            e.Property(x => x.Rut).HasMaxLength(12);
            e.HasIndex(x => x.Rut).IsUnique();
            e.Property(x => x.Nombres).HasMaxLength(80);
            e.Property(x => x.Apellidos).HasMaxLength(80);
            e.Property(x => x.Direccion).HasMaxLength(200);
            e.Property(x => x.Estado).HasMaxLength(15);
            e.HasOne(x => x.Unidad).WithMany(u => u.Beneficiarios).HasForeignKey(x => x.UnidadId);
            e.HasOne(x => x.Patrulla).WithMany().HasForeignKey(x => x.PatrullaId);
            e.ConFechas();
        });

        builder.Entity<FichaMedica>(e =>
        {
            e.ToTable("fichas_medicas");
            // Todos los datos de salud se guardan cifrados (ASP.NET Data Protection), por eso las columnas son
            // de texto sin largo fijo; los largos máximos se validan antes de cifrar. En la bitácora se registran
            // como "(dato protegido)".
            e.Property(x => x.Prevision).Sensible();
            e.Property(x => x.GrupoSanguineo).Sensible();
            e.Property(x => x.Alergias).Sensible();
            e.Property(x => x.Enfermedades).Sensible();
            e.Property(x => x.Medicamentos).Sensible();
            e.Property(x => x.RestriccionesAlimentarias).Sensible();
            e.Property(x => x.NecesidadesEmocionales).Sensible();
            e.HasIndex(x => x.BeneficiarioId).IsUnique();
            e.HasOne(x => x.Beneficiario).WithOne(b => b.FichaMedica)
                .HasForeignKey<FichaMedica>(x => x.BeneficiarioId).OnDelete(DeleteBehavior.Cascade);
            // SET NULL: si se elimina la cuenta de quien modificó la ficha, la ficha se conserva
            // (schema.sql original usaba CASCADE y borraba la ficha del beneficiario).
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.ActualizadoPor).OnDelete(DeleteBehavior.SetNull);
            e.ConFechas();
        });

        builder.Entity<ContactoEmergencia>(e =>
        {
            e.ToTable("contactos_emergencia");
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Parentesco).HasMaxLength(40);
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.HasOne(x => x.Beneficiario).WithMany(b => b.ContactosEmergencia)
                .HasForeignKey(x => x.BeneficiarioId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Apoderado>(e =>
        {
            e.ToTable("apoderados");
            e.Property(x => x.Rut).HasMaxLength(12);
            e.HasIndex(x => x.Rut).IsUnique();
            e.HasIndex(x => x.UsuarioId).IsUnique();
            e.Property(x => x.Nombres).HasMaxLength(80);
            e.Property(x => x.Apellidos).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
            e.ConFechas();
        });

        builder.Entity<BeneficiarioApoderado>(e =>
        {
            e.ToTable("beneficiario_apoderado");
            e.HasKey(x => new { x.BeneficiarioId, x.ApoderadoId });
            e.Property(x => x.Parentesco).HasMaxLength(40);
            e.HasOne(x => x.Beneficiario).WithMany(b => b.Apoderados)
                .HasForeignKey(x => x.BeneficiarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Apoderado).WithMany(a => a.Beneficiarios)
                .HasForeignKey(x => x.ApoderadoId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Voluntario>(e =>
        {
            e.ToTable("voluntarios");
            e.Property(x => x.Rut).HasMaxLength(12);
            e.HasIndex(x => x.Rut).IsUnique();
            e.HasIndex(x => x.UsuarioId).IsUnique();
            e.Property(x => x.Nombres).HasMaxLength(80);
            e.Property(x => x.Apellidos).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(120);
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
            e.ConFechas();
        });

        builder.Entity<CargoVoluntario>(e =>
        {
            e.ToTable("cargos_voluntario");
            e.Property(x => x.Cargo).HasMaxLength(60);
            e.HasOne(x => x.Voluntario).WithMany(v => v.Cargos).HasForeignKey(x => x.VoluntarioId);
            e.HasOne(x => x.Unidad).WithMany().HasForeignKey(x => x.UnidadId);
        });

        builder.Entity<HistorialBeneficiario>(e =>
        {
            e.ToTable("historial_beneficiario", t => t.HasCheckConstraint("ck_historial_beneficiario_tipo",
                EnLista("tipo", "ingreso", "cambio_unidad", "cambio_patrulla", "retiro", "reingreso")));
            e.Property(x => x.Tipo).HasMaxLength(20);
            e.Property(x => x.Detalle).HasMaxLength(255);
            e.HasOne(x => x.Beneficiario).WithMany().HasForeignKey(x => x.BeneficiarioId);
            e.HasOne<Unidad>().WithMany().HasForeignKey(x => x.UnidadOrigenId);
            e.HasOne<Unidad>().WithMany().HasForeignKey(x => x.UnidadDestinoId);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
        });
    }
}

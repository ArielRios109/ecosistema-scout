using EcosistemaScout.Api.Modulos.Base;
using EcosistemaScout.Api.Modulos.Finanzas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Datos;

/// <summary>
/// Carga los datos iniciales de schema.sql (roles, plan de cuentas y centro de costo del grupo)
/// y, solo en desarrollo, un usuario de demostración por rol. Es idempotente: se puede ejecutar varias veces.
/// </summary>
public class DatosIniciales(
    AppDbContext db,
    RoleManager<Rol> roles,
    UserManager<Usuario> usuarios,
    IConfiguration configuracion,
    ILogger<DatosIniciales> log)
{
    private static readonly (string Nombre, string Descripcion)[] RolesSistema =
    [
        (Roles.Administrador, "Configura el grupo, usuarios, unidades y voluntarios"),
        (Roles.Tesoreria, "Registra movimientos, genera cargos, revisa pagos y lleva el inventario"),
        (Roles.Dirigente, "Gestiona beneficiarios y apoderados de su unidad y registra préstamos"),
        (Roles.Apoderado, "Usa el portal: estado de cuenta, pagos informados y datos de sus hijos"),
        (Roles.Revisor, "Comisión revisora: consulta finanzas e inventario en solo lectura"),
    ];

    // Nota de schema.sql: los códigos son una propuesta; se ajustan al formulario DAF-FOR-07 vigente del grupo.
    private static readonly (string Codigo, string Nombre, string Tipo)[] PlanDeCuentas =
    [
        ("1.1", "Saldo inicial", "ingreso"),
        ("2.1", "Cuotas de registro institucional", "ingreso"),
        ("2.2", "Cuotas mensuales de grupo", "ingreso"),
        ("3.1", "Cuotas de campamento", "ingreso"),
        ("3.2", "Cuotas de actividades y salidas", "ingreso"),
        ("4.1", "Venta de indumentaria e insignias", "ingreso"),
        ("5.1", "Donaciones y aportes", "ingreso"),
        ("6.1", "Actividades para reunir fondos", "ingreso"),
        ("7.1", "Pago de registro institucional a la AGSCh", "egreso"),
        ("7.2", "Gastos de campamentos", "egreso"),
        ("7.3", "Gastos de actividades y salidas", "egreso"),
        ("7.4", "Materiales de programa", "egreso"),
        ("7.5", "Compra de equipamiento e inventario", "egreso"),
        ("7.6", "Compra de indumentaria e insignias para venta", "egreso"),
        ("7.7", "Sede, servicios básicos y mantención", "egreso"),
        ("7.8", "Formación de dirigentes", "egreso"),
        ("7.9", "Gastos de administración", "egreso"),
    ];

    private static readonly (string Email, string Nombre, string Rol)[] UsuariosDemo =
    [
        ("admin@demo.cl", "Administración Demo", Roles.Administrador),
        ("tesoreria@demo.cl", "Tesorería Demo", Roles.Tesoreria),
        ("dirigente@demo.cl", "Dirigente Demo", Roles.Dirigente),
        ("apoderado@demo.cl", "Apoderado Demo", Roles.Apoderado),
        ("revisor@demo.cl", "Revisor Demo", Roles.Revisor),
    ];

    public async Task CargarAsync(bool incluirUsuariosDemo)
    {
        foreach (var (nombre, descripcion) in RolesSistema)
        {
            if (!await roles.RoleExistsAsync(nombre))
            {
                await roles.CreateAsync(new Rol { Name = nombre, Descripcion = descripcion });
            }
        }

        var codigosExistentes = await db.ItemsPresupuestarios.Select(i => i.Codigo).ToListAsync();
        db.ItemsPresupuestarios.AddRange(PlanDeCuentas
            .Where(i => !codigosExistentes.Contains(i.Codigo))
            .Select(i => new ItemPresupuestario { Codigo = i.Codigo, Nombre = i.Nombre, Tipo = i.Tipo }));

        if (!await db.CentrosCosto.AnyAsync(c => c.Tipo == "grupo"))
        {
            db.CentrosCosto.Add(new CentroCosto { Nombre = "Grupo (gastos generales)", Tipo = "grupo" });
        }

        await db.SaveChangesAsync();

        if (incluirUsuariosDemo)
        {
            await CargarUsuariosDemoAsync();
        }
    }

    private async Task CargarUsuariosDemoAsync()
    {
        var password = configuracion["Seed:PasswordDemo"];
        if (string.IsNullOrWhiteSpace(password))
        {
            log.LogWarning("No se crearon usuarios de demostración: falta configurar Seed:PasswordDemo.");
            return;
        }

        foreach (var (email, nombre, rol) in UsuariosDemo)
        {
            if (await usuarios.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var usuario = new Usuario { UserName = email, Email = email, EmailConfirmed = true, Nombre = nombre };
            var resultado = await usuarios.CreateAsync(usuario, password);
            if (!resultado.Succeeded)
            {
                log.LogError("No se pudo crear el usuario demo {Email}: {Errores}", email,
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
                continue;
            }
            await usuarios.AddToRoleAsync(usuario, rol);
        }
    }
}

using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using EcosistemaScout.Api.Modulos.Miembros;
using EcosistemaScout.Api.Modulos.Miembros.Beneficiarios;
using EcosistemaScout.Api.Modulos.Miembros.FichasMedicas;
using EcosistemaScout.Api.Modulos.Miembros.Unidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Base.Inicio;

public record ConteoRama(string Rama, int Beneficiarios);

public record CupoUnidad(long Id, string Nombre, string Rama, int Activos, int Maximo);

/// <summary>Cifras de Miembros. Si es un dirigente con cargo en una unidad, solo de esa unidad (Alcance la nombra).</summary>
public record ResumenMiembros(
    string Alcance, long? UnidadId, int BeneficiariosActivos, int Voluntarios, IReadOnlyList<ConteoRama> PorRama,
    IReadOnlyList<CupoUnidad> Unidades, int FichasDesactualizadas, int SinFicha, int Egresos, int SobreEdadMaxima);

public record PrestamoVencidoBreve(long BienId, string Bien, string Destinatario, DateOnly FechaDevolucionEsperada);

public record ResumenInventario(int Bienes, int PrestamosActivos, int PrestamosVencidos, IReadOnlyList<PrestamoVencidoBreve> Vencidos);

public record ResumenUsuarios(int Activos, int ConPasswordTemporal);

public record BeneficiarioDeApoderado(long Id, string Nombre, string Unidad, string Parentesco, string? AvisoEdad, string EstadoFicha);

/// <summary>Lo que muestra la página de inicio. Cada sección viene solo si el rol de la persona puede verla.</summary>
public record ResumenInicio(
    ResumenMiembros? Miembros, ResumenInventario? Inventario, ResumenUsuarios? Usuarios,
    IReadOnlyList<BeneficiarioDeApoderado>? MisBeneficiarios, bool FinanzasProximamente);

/// <summary>Resumen de la página de inicio, armado según las políticas de cada módulo.</summary>
[ApiController]
[Route("api/base/inicio")]
[Authorize]
public class InicioController(AppDbContext db, IAuthorizationService autorizacion, IUsuarioActual usuarioActual) : ControllerBase
{
    /// <summary>Unidades con este cupo ocupado o más se marcan como "cerca del máximo".</summary>
    public const int UmbralCupo = 28;

    [HttpGet]
    public async Task<ResponseData<ResumenInicio>> Obtener(CancellationToken ct)
    {
        // La comisión revisora ve las cifras agregadas de Miembros (sin datos personales), aunque no entre al módulo.
        var verMiembros = await PuedeAsync(Politicas.Miembros) || User.IsInRole(Roles.Revisor);
        var esApoderado = User.IsInRole(Roles.Apoderado);

        return ResponseData.Ok(new ResumenInicio(
            verMiembros ? await MiembrosAsync(ct) : null,
            await PuedeAsync(Politicas.Inventario) ? await InventarioAsync(ct) : null,
            await PuedeAsync(Politicas.Administracion) ? await UsuariosAsync(ct) : null,
            esApoderado ? await MisBeneficiariosAsync(ct) : null,
            await PuedeAsync(Politicas.Finanzas)));
    }

    private async Task<ResumenMiembros> MiembrosAsync(CancellationToken ct)
    {
        // Un dirigente (que no sea administrador) con un cargo vigente en una unidad ve solo esa unidad.
        var unidad = !User.IsInRole(Roles.Administrador) && User.IsInRole(Roles.Dirigente) ? await UnidadDelDirigenteAsync(ct) : null;
        var unidadId = unidad?.Id;

        var unidades = db.Unidades.AsNoTracking().Where(u => u.Activa && (unidadId == null || u.Id == unidadId));
        var activos = db.Beneficiarios.AsNoTracking()
            .Where(b => b.Estado == EstadosBeneficiario.Activo && (unidadId == null || b.UnidadId == unidadId));

        var cupos = await unidades
            .OrderBy(u => u.Nombre)
            .Select(u => new CupoUnidad(u.Id, u.Nombre, u.Rama, u.Beneficiarios.Count(b => b.Estado == EstadosBeneficiario.Activo), Ramas.MaximoPorUnidad))
            .ToListAsync(ct);
        var porRama = await activos
            .GroupBy(b => b.Unidad.Rama)
            .Select(g => new ConteoRama(g.Key, g.Count()))
            .ToListAsync(ct);

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var voluntarios = await db.Voluntarios.CountAsync(v => v.Cargos.Any(c =>
            (c.FechaTermino == null || c.FechaTermino >= hoy) && (unidadId == null || c.UnidadId == unidadId)), ct);

        return new ResumenMiembros(
            unidad?.Nombre ?? "Todo el grupo",
            unidadId,
            await activos.CountAsync(ct),
            voluntarios,
            [.. porRama.OrderBy(r => Ramas.Todas.ToList().IndexOf(r.Rama))],
            cupos,
            await AvisosBeneficiario.Filtrar(activos, AvisosBeneficiario.FichaDesactualizada).CountAsync(ct),
            await AvisosBeneficiario.Filtrar(activos, AvisosBeneficiario.SinFicha).CountAsync(ct),
            await AvisosBeneficiario.Filtrar(activos, AvisosBeneficiario.Egreso).CountAsync(ct),
            await AvisosBeneficiario.Filtrar(activos, AvisosBeneficiario.EdadMaxima).CountAsync(ct));
    }

    private async Task<(long Id, string Nombre)?> UnidadDelDirigenteAsync(CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var cargo = await db.CargosVoluntario.AsNoTracking()
            .Where(c => c.Voluntario.UsuarioId == usuarioActual.Id && c.UnidadId != null && (c.FechaTermino == null || c.FechaTermino >= hoy))
            .OrderByDescending(c => c.FechaInicio)
            .Select(c => new { Id = c.UnidadId!.Value, c.Unidad!.Nombre })
            .FirstOrDefaultAsync(ct);
        return cargo is null ? null : (cargo.Id, cargo.Nombre);
    }

    private async Task<ResumenInventario> InventarioAsync(CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var activos = db.Prestamos.AsNoTracking().Where(p => p.FechaDevolucion == null);
        var vencidos = activos.Where(p => p.FechaDevolucionEsperada < hoy);

        return new ResumenInventario(
            await db.Bienes.CountAsync(b => !b.DadoDeBaja, ct),
            await activos.CountAsync(ct),
            await vencidos.CountAsync(ct),
            await vencidos
                .OrderBy(p => p.FechaDevolucionEsperada)
                .Take(5)
                .Select(p => new PrestamoVencidoBreve(p.BienId, p.Bien.Nombre, p.Destinatario, p.FechaDevolucionEsperada))
                .ToListAsync(ct));
    }

    private async Task<ResumenUsuarios> UsuariosAsync(CancellationToken ct) =>
        new(await db.Users.CountAsync(u => u.Activo, ct), await db.Users.CountAsync(u => u.Activo && u.DebeCambiarPassword, ct));

    private async Task<IReadOnlyList<BeneficiarioDeApoderado>> MisBeneficiariosAsync(CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var vinculos = await db.BeneficiarioApoderado.AsNoTracking()
            .Where(v => v.Apoderado.UsuarioId == usuarioActual.Id && v.Beneficiario.Estado == EstadosBeneficiario.Activo)
            .OrderBy(v => v.Beneficiario.Nombres)
            .Select(v => new
            {
                v.BeneficiarioId,
                Nombre = v.Beneficiario.Nombres + " " + v.Beneficiario.Apellidos,
                Unidad = v.Beneficiario.Unidad.Nombre,
                v.Parentesco,
                v.Beneficiario.FechaNacimiento,
                FichaActualizada = v.Beneficiario.FichaMedica != null ? (DateTime?)v.Beneficiario.FichaMedica.UpdatedAt : null,
            })
            .ToListAsync(ct);

        return [.. vinculos.Select(v => new BeneficiarioDeApoderado(v.BeneficiarioId, v.Nombre, v.Unidad, v.Parentesco,
            Edades.Aviso(BeneficiariosController.Edad(v.FechaNacimiento, hoy)), VigenciaFicha.Estado(v.FichaActualizada)))];
    }

    private async Task<bool> PuedeAsync(string politica) => (await autorizacion.AuthorizeAsync(User, politica)).Succeeded;
}

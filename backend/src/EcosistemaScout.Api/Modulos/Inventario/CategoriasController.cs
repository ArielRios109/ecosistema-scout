using System.ComponentModel.DataAnnotations;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Inventario;

public record CategoriaDto(long Id, string Nombre, int Bienes);

public record SolicitudCategoria(
    [Required(ErrorMessage = "Ingresa el nombre de la categoría.")]
    [MaxLength(60, ErrorMessage = "El nombre admite hasta 60 caracteres.")]
    string Nombre);

/// <summary>Categorías de bienes (carpas, cocina, herramientas...). Solo se eliminan si no tienen bienes.</summary>
[ApiController]
[Route("api/inventario/categorias")]
[Authorize(Policy = Politicas.Inventario)]
public class CategoriasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ResponseData<List<CategoriaDto>>> Listar(CancellationToken ct) =>
        ResponseData.Ok(await db.CategoriasBien.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.Id, c.Nombre, c.Bienes.Count))
            .ToListAsync(ct));

    [HttpPost]
    [Authorize(Policy = Politicas.InventarioGestion)]
    public async Task<IActionResult> Crear(SolicitudCategoria solicitud, CancellationToken ct)
    {
        var nombre = solicitud.Nombre.Trim();
        if (await NombreUsadoAsync(nombre, null, ct)) return Repetida();

        var categoria = new CategoriaBien { Nombre = nombre };
        db.CategoriasBien.Add(categoria);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(new CategoriaDto(categoria.Id, categoria.Nombre, 0), "Categoría creada."));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Politicas.InventarioGestion)]
    public async Task<IActionResult> Editar(long id, SolicitudCategoria solicitud, CancellationToken ct)
    {
        var categoria = await db.CategoriasBien.FindAsync([id], ct);
        if (categoria is null) return NoEncontrada();
        var nombre = solicitud.Nombre.Trim();
        if (await NombreUsadoAsync(nombre, id, ct)) return Repetida();

        categoria.Nombre = nombre;
        await db.SaveChangesAsync(ct);
        var bienes = await db.Bienes.CountAsync(b => b.CategoriaId == id, ct);
        return Ok(ResponseData.Ok(new CategoriaDto(id, nombre, bienes), "Categoría actualizada."));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Politicas.InventarioGestion)]
    public async Task<IActionResult> Eliminar(long id, CancellationToken ct)
    {
        var categoria = await db.CategoriasBien.FindAsync([id], ct);
        if (categoria is null) return NoEncontrada();
        if (await db.Bienes.AnyAsync(b => b.CategoriaId == id, ct))
        {
            return Conflict(ResponseData.Error("La categoría tiene bienes. Muévelos a otra categoría antes de eliminarla."));
        }

        db.CategoriasBien.Remove(categoria);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok<object?>(null, "Categoría eliminada."));
    }

    private Task<bool> NombreUsadoAsync(string nombre, long? excluirId, CancellationToken ct) =>
        db.CategoriasBien.AnyAsync(c => c.Id != excluirId && c.Nombre.ToLower() == nombre.ToLower(), ct);

    private ConflictObjectResult Repetida() => Conflict(ResponseData.Error("Ya existe una categoría con ese nombre."));
    private NotFoundObjectResult NoEncontrada() => NotFound(ResponseData.Error("No se encontró la categoría."));
}

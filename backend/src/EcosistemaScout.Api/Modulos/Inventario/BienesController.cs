using System.ComponentModel.DataAnnotations;
using ClosedXML.Excel;
using EcosistemaScout.Api.Comun;
using EcosistemaScout.Api.Datos;
using EcosistemaScout.Api.Modulos.Base.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Modulos.Inventario;

public class FiltroBienes : ConsultaPaginada
{
    public long? CategoriaId { get; set; }
    public string? Estado { get; set; }
    public string? Ubicacion { get; set; }
    /// <summary>Por defecto el listado oculta los bienes dados de baja.</summary>
    public bool IncluirBajas { get; set; }
}

public record BienResumen(
    long Id, string Codigo, string Nombre, long CategoriaId, string Categoria, int Cantidad, int Disponible,
    string Estado, string? Ubicacion, string? Responsable, bool DadoDeBaja, bool TieneFoto, bool TienePrestamoVencido);

public record BajaDto(string Motivo, DateOnly Fecha, string? Detalle, string? RegistradaPor);

public record BienDetalle(
    long Id, string Codigo, string Nombre, long CategoriaId, string Categoria, int Cantidad, int Disponible,
    string? Marca, string? NumeroSerie, string? Descripcion, DateOnly? FechaAdquisicion, int? PrecioCompra,
    int? ValorEstimado, string Origen, string Estado, string? Ubicacion, long? ResponsableId, string? Responsable,
    bool DadoDeBaja, bool TieneFoto, IReadOnlyList<PrestamoDto> Prestamos, BajaDto? Baja);

public record SolicitudBien(
    [Required(ErrorMessage = "Ingresa el nombre del bien.")][MaxLength(120, ErrorMessage = "El nombre admite hasta 120 caracteres.")] string Nombre,
    long CategoriaId,
    [Range(1, 100_000, ErrorMessage = "La cantidad debe ser al menos 1.")] int Cantidad,
    [MaxLength(60, ErrorMessage = "La marca admite hasta 60 caracteres.")] string? Marca,
    [MaxLength(60, ErrorMessage = "El número de serie admite hasta 60 caracteres.")] string? NumeroSerie,
    [MaxLength(1000, ErrorMessage = "La descripción admite hasta 1000 caracteres.")] string? Descripcion,
    DateOnly? FechaAdquisicion,
    [Range(0, int.MaxValue, ErrorMessage = "El precio de compra no puede ser negativo.")] int? PrecioCompra,
    [Range(0, int.MaxValue, ErrorMessage = "El valor estimado no puede ser negativo.")] int? ValorEstimado,
    [Required(ErrorMessage = "Indica el origen del bien.")] string Origen,
    [Required(ErrorMessage = "Indica el estado del bien.")] string Estado,
    [MaxLength(120, ErrorMessage = "La ubicación admite hasta 120 caracteres.")] string? Ubicacion,
    long? ResponsableId);

public record SolicitudBaja(
    [Required(ErrorMessage = "Indica el motivo de la baja.")] string Motivo,
    DateOnly? Fecha,
    [MaxLength(255, ErrorMessage = "El detalle admite hasta 255 caracteres.")] string? Detalle);

/// <summary>
/// Bienes del grupo (formulario DAF-FOR-04). Los ven todos los roles con acceso a Inventario; los administran
/// administración y tesorería. Un bien nunca se borra: se da de baja y queda en el historial.
/// </summary>
[ApiController]
[Route("api/inventario/bienes")]
[Authorize(Policy = Politicas.Inventario)]
public class BienesController(AppDbContext db, IUsuarioActual usuarioActual, IAlmacenArchivos archivos) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] FiltroBienes filtro, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Estado) && !ValoresInventario.Estados.Contains(filtro.Estado))
        {
            return BadRequest(ResponseData.Error("El estado no es válido."));
        }

        var hoy = Hoy();
        var pagina = await Filtrar(filtro)
            .OrderBy(b => b.Nombre).ThenBy(b => b.Codigo)
            .Select(b => new BienResumen(
                b.Id, b.Codigo, b.Nombre, b.CategoriaId, b.Categoria.Nombre, b.Cantidad,
                b.Cantidad - b.Prestamos.Where(p => p.FechaDevolucion == null).Sum(p => p.Cantidad),
                b.Estado, b.Ubicacion,
                db.Voluntarios.Where(v => v.Id == b.ResponsableId).Select(v => v.Nombres + " " + v.Apellidos).FirstOrDefault(),
                b.DadoDeBaja, b.FotoPath != null,
                b.Prestamos.Any(p => p.FechaDevolucion == null && p.FechaDevolucionEsperada < hoy)))
            .PaginarAsync(filtro, ct);
        return Ok(ResponseData.Ok(pagina));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id, CancellationToken ct)
    {
        var detalle = await DetalleAsync(id, ct);
        return detalle is null ? NoEncontrado() : Ok(ResponseData.Ok(detalle));
    }

    [HttpPost]
    [Authorize(Policy = Politicas.InventarioGestion)]
    public async Task<IActionResult> Crear(SolicitudBien solicitud, CancellationToken ct)
    {
        if (await ValidarAsync(solicitud, ct) is { } error) return error;

        var bien = new Bien { Codigo = await SiguienteCodigoAsync(ct) };
        Copiar(solicitud, bien);
        db.Bienes.Add(bien);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Obtener), new { id = bien.Id },
            ResponseData.Ok(await DetalleAsync(bien.Id, ct), $"Bien {bien.Codigo} registrado."));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Politicas.InventarioGestion)]
    public async Task<IActionResult> Editar(long id, SolicitudBien solicitud, CancellationToken ct)
    {
        var bien = await db.Bienes.FindAsync([id], ct);
        if (bien is null) return NoEncontrado();
        if (bien.DadoDeBaja) return Conflict(ResponseData.Error("El bien está dado de baja y ya no se puede modificar."));
        if (await ValidarAsync(solicitud, ct) is { } error) return error;

        var prestada = await CantidadPrestadaAsync(id, ct);
        if (solicitud.Cantidad < prestada)
        {
            return Conflict(ResponseData.Error($"Hay {prestada} en préstamo: la cantidad no puede ser menor."));
        }

        Copiar(solicitud, bien);
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Bien actualizado."));
    }

    /// <summary>Da de baja el bien (venta, robo, deterioro...). Se conserva con su historial; no puede tener préstamos activos.</summary>
    [HttpPost("{id:long}/baja")]
    [Authorize(Policy = Politicas.InventarioGestion)]
    public async Task<IActionResult> DarDeBaja(long id, SolicitudBaja solicitud, CancellationToken ct)
    {
        var bien = await db.Bienes.FindAsync([id], ct);
        if (bien is null) return NoEncontrado();
        if (bien.DadoDeBaja) return Conflict(ResponseData.Error("El bien ya está dado de baja."));
        if (!ValoresInventario.MotivosBaja.Contains(solicitud.Motivo))
        {
            return BadRequest(ResponseData.Error("El motivo de baja no es válido."));
        }
        if (await CantidadPrestadaAsync(id, ct) > 0)
        {
            return Conflict(ResponseData.Error("El bien tiene préstamos sin devolver. Registra la devolución antes de darlo de baja."));
        }

        var fecha = solicitud.Fecha ?? Hoy();
        if (fecha > Hoy()) return BadRequest(ResponseData.Error("La fecha de baja no puede ser futura."));

        bien.DadoDeBaja = true;
        db.BajasBien.Add(new BajaBien
        {
            BienId = id,
            Motivo = solicitud.Motivo,
            Fecha = fecha,
            Detalle = string.IsNullOrWhiteSpace(solicitud.Detalle) ? null : solicitud.Detalle.Trim(),
            RegistradoPor = usuarioActual.Id!.Value,
        });
        await db.SaveChangesAsync(ct);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Bien dado de baja."));
    }

    // ----- Foto -----

    /// <summary>Sube o reemplaza la foto (JPG, PNG o WebP de hasta 3 MB).</summary>
    [HttpPut("{id:long}/foto")]
    [Authorize(Policy = Politicas.InventarioGestion)]
    [RequestSizeLimit(ValoresInventario.TamanoMaximoFoto + 64 * 1024)]
    public async Task<IActionResult> SubirFoto(long id, IFormFile? foto, CancellationToken ct)
    {
        var bien = await db.Bienes.FindAsync([id], ct);
        if (bien is null) return NoEncontrado();
        if (foto is null || foto.Length == 0) return BadRequest(ResponseData.Error("Selecciona una foto."));
        if (foto.Length > ValoresInventario.TamanoMaximoFoto) return BadRequest(ResponseData.Error("La foto puede pesar hasta 3 MB."));
        if (!ValoresInventario.TiposFoto.TryGetValue(foto.ContentType, out var extension) || !await EsImagenAsync(foto, ct))
        {
            return BadRequest(ResponseData.Error("La foto debe ser JPG, PNG o WebP."));
        }

        await using var contenido = foto.OpenReadStream();
        var anterior = bien.FotoPath;
        bien.FotoPath = await archivos.GuardarAsync(contenido, "bienes", extension, ct);
        await db.SaveChangesAsync(ct);
        if (anterior is not null) archivos.Eliminar(anterior);
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Foto guardada."));
    }

    /// <summary>Entrega la foto. El frontend la pide con su token y la muestra como imagen.</summary>
    [HttpGet("{id:long}/foto")]
    public async Task<IActionResult> Foto(long id, CancellationToken ct)
    {
        var ruta = await db.Bienes.Where(b => b.Id == id).Select(b => b.FotoPath).FirstOrDefaultAsync(ct);
        var contenido = ruta is null ? null : archivos.Abrir(ruta);
        if (contenido is null) return NotFound(ResponseData.Error("El bien no tiene foto."));

        var tipo = ValoresInventario.TiposFoto.First(t => ruta!.EndsWith(t.Value)).Key;
        return File(contenido, tipo);
    }

    [HttpDelete("{id:long}/foto")]
    [Authorize(Policy = Politicas.InventarioGestion)]
    public async Task<IActionResult> EliminarFoto(long id, CancellationToken ct)
    {
        var bien = await db.Bienes.FindAsync([id], ct);
        if (bien is null) return NoEncontrado();
        if (bien.FotoPath is { } ruta)
        {
            bien.FotoPath = null;
            await db.SaveChangesAsync(ct);
            archivos.Eliminar(ruta);
        }
        return Ok(ResponseData.Ok(await DetalleAsync(id, ct), "Foto eliminada."));
    }

    // ----- Exportación -----

    /// <summary>Planilla Excel con los bienes que cumplen los filtros (sin paginar), con las columnas del DAF-FOR-04.</summary>
    [HttpGet("exportar")]
    public async Task<IActionResult> Exportar([FromQuery] FiltroBienes filtro, CancellationToken ct)
    {
        var bienes = await Filtrar(filtro)
            .OrderBy(b => b.Codigo)
            .Select(b => new
            {
                b.Codigo, b.Nombre, Categoria = b.Categoria.Nombre, b.Cantidad, b.Marca, b.NumeroSerie, b.Descripcion,
                b.FechaAdquisicion, b.PrecioCompra, b.ValorEstimado, b.Origen, b.Estado, b.Ubicacion,
                Responsable = db.Voluntarios.Where(v => v.Id == b.ResponsableId).Select(v => v.Nombres + " " + v.Apellidos).FirstOrDefault(),
                b.DadoDeBaja,
            })
            .ToListAsync(ct);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Inventario");
        string[] columnas =
        [
            "Código", "Nombre", "Categoría", "Cantidad", "Marca", "N° de serie", "Descripción", "Fecha de adquisición",
            "Precio de compra", "Valor estimado", "Origen", "Estado", "Ubicación", "Responsable", "Dado de baja",
        ];
        for (var c = 0; c < columnas.Length; c++) hoja.Cell(1, c + 1).Value = columnas[c];

        var fila = 2;
        foreach (var b in bienes)
        {
            hoja.Cell(fila, 1).Value = b.Codigo;
            hoja.Cell(fila, 2).Value = b.Nombre;
            hoja.Cell(fila, 3).Value = b.Categoria;
            hoja.Cell(fila, 4).Value = b.Cantidad;
            hoja.Cell(fila, 5).Value = b.Marca;
            hoja.Cell(fila, 6).Value = b.NumeroSerie;
            hoja.Cell(fila, 7).Value = b.Descripcion;
            if (b.FechaAdquisicion is { } fecha) hoja.Cell(fila, 8).Value = fecha.ToDateTime(TimeOnly.MinValue);
            if (b.PrecioCompra is { } precio) hoja.Cell(fila, 9).Value = precio;
            if (b.ValorEstimado is { } valor) hoja.Cell(fila, 10).Value = valor;
            hoja.Cell(fila, 11).Value = b.Origen;
            hoja.Cell(fila, 12).Value = b.Estado;
            hoja.Cell(fila, 13).Value = b.Ubicacion;
            hoja.Cell(fila, 14).Value = b.Responsable;
            hoja.Cell(fila, 15).Value = b.DadoDeBaja ? "Sí" : "No";
            fila++;
        }

        hoja.Column(8).Style.DateFormat.Format = "dd-mm-yyyy";
        hoja.Columns(9, 10).Style.NumberFormat.Format = "$ #,##0";
        hoja.Row(1).Style.Font.Bold = true;
        hoja.Range(1, 1, Math.Max(fila - 1, 1), columnas.Length).SetAutoFilter();
        hoja.SheetView.FreezeRows(1);
        hoja.Columns().AdjustToContents(1, 200);

        var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        memoria.Position = 0;
        return File(memoria, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"inventario-{Hoy():yyyy-MM-dd}.xlsx");
    }

    // ----- Apoyo -----

    private IQueryable<Bien> Filtrar(FiltroBienes filtro)
    {
        var consulta = db.Bienes.AsNoTracking();
        if (!filtro.IncluirBajas) consulta = consulta.Where(b => !b.DadoDeBaja);
        if (filtro.BuscarNormalizado is { } texto)
        {
            consulta = consulta.Where(b => b.Nombre.ToLower().Contains(texto) || b.Codigo.ToLower().Contains(texto)
                || (b.Marca != null && b.Marca.ToLower().Contains(texto))
                || (b.NumeroSerie != null && b.NumeroSerie.ToLower().Contains(texto)));
        }
        if (filtro.CategoriaId is { } categoriaId) consulta = consulta.Where(b => b.CategoriaId == categoriaId);
        if (!string.IsNullOrWhiteSpace(filtro.Estado)) consulta = consulta.Where(b => b.Estado == filtro.Estado);
        if (!string.IsNullOrWhiteSpace(filtro.Ubicacion))
        {
            var ubicacion = filtro.Ubicacion.Trim().ToLower();
            consulta = consulta.Where(b => b.Ubicacion != null && b.Ubicacion.ToLower().Contains(ubicacion));
        }
        return consulta;
    }

    private async Task<BienDetalle?> DetalleAsync(long id, CancellationToken ct)
    {
        var b = await db.Bienes.AsNoTracking().Include(x => x.Categoria).Include(x => x.Baja).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null) return null;

        var prestamos = await PrestamosController.ConsultarAsync(db, db.Prestamos.Where(p => p.BienId == id), ct);
        var prestada = prestamos.Where(p => p.FechaDevolucion is null).Sum(p => p.Cantidad);
        var responsable = b.ResponsableId is { } rid
            ? await db.Voluntarios.Where(v => v.Id == rid).Select(v => v.Nombres + " " + v.Apellidos).FirstOrDefaultAsync(ct)
            : null;
        var baja = b.Baja is null
            ? null
            : new BajaDto(b.Baja.Motivo, b.Baja.Fecha, b.Baja.Detalle,
                await db.Users.Where(u => u.Id == b.Baja.RegistradoPor).Select(u => u.Nombre).FirstOrDefaultAsync(ct));

        return new BienDetalle(b.Id, b.Codigo, b.Nombre, b.CategoriaId, b.Categoria.Nombre, b.Cantidad, b.Cantidad - prestada,
            b.Marca, b.NumeroSerie, b.Descripcion, b.FechaAdquisicion, b.PrecioCompra, b.ValorEstimado, b.Origen, b.Estado,
            b.Ubicacion, b.ResponsableId, responsable, b.DadoDeBaja, b.FotoPath is not null, prestamos, baja);
    }

    private async Task<IActionResult?> ValidarAsync(SolicitudBien solicitud, CancellationToken ct)
    {
        if (!ValoresInventario.Origenes.Contains(solicitud.Origen)) return BadRequest(ResponseData.Error("El origen no es válido."));
        if (!ValoresInventario.Estados.Contains(solicitud.Estado)) return BadRequest(ResponseData.Error("El estado no es válido."));
        if (solicitud.FechaAdquisicion > Hoy()) return BadRequest(ResponseData.Error("La fecha de adquisición no puede ser futura."));
        if (!await db.CategoriasBien.AnyAsync(c => c.Id == solicitud.CategoriaId, ct))
        {
            return BadRequest(ResponseData.Error("Selecciona una categoría válida."));
        }
        if (solicitud.ResponsableId is { } responsable && !await db.Voluntarios.AnyAsync(v => v.Id == responsable, ct))
        {
            return BadRequest(ResponseData.Error("El responsable debe ser un voluntario registrado."));
        }
        return null;
    }

    /// <summary>Código correlativo "INV-00001". Los bienes nunca se borran, así que el conteo no repite códigos.</summary>
    private async Task<string> SiguienteCodigoAsync(CancellationToken ct) => $"INV-{await db.Bienes.CountAsync(ct) + 1:D5}";

    private Task<int> CantidadPrestadaAsync(long bienId, CancellationToken ct) =>
        db.Prestamos.Where(p => p.BienId == bienId && p.FechaDevolucion == null).SumAsync(p => p.Cantidad, ct);

    /// <summary>Revisa los primeros bytes del archivo: el tipo que declara el navegador se puede falsear.</summary>
    private static async Task<bool> EsImagenAsync(IFormFile archivo, CancellationToken ct)
    {
        var cabecera = new byte[12];
        await using var lectura = archivo.OpenReadStream();
        var leidos = await lectura.ReadAsync(cabecera, ct);
        if (leidos < 12) return false;
        var jpg = cabecera[0] == 0xFF && cabecera[1] == 0xD8;
        var png = cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47;
        var webp = cabecera[0] == 'R' && cabecera[1] == 'I' && cabecera[2] == 'F' && cabecera[3] == 'F'
            && cabecera[8] == 'W' && cabecera[9] == 'E' && cabecera[10] == 'B' && cabecera[11] == 'P';
        return jpg || png || webp;
    }

    private static void Copiar(SolicitudBien s, Bien bien)
    {
        bien.Nombre = s.Nombre.Trim();
        bien.CategoriaId = s.CategoriaId;
        bien.Cantidad = s.Cantidad;
        bien.Marca = Limpio(s.Marca);
        bien.NumeroSerie = Limpio(s.NumeroSerie);
        bien.Descripcion = Limpio(s.Descripcion);
        bien.FechaAdquisicion = s.FechaAdquisicion;
        bien.PrecioCompra = s.PrecioCompra;
        bien.ValorEstimado = s.ValorEstimado;
        bien.Origen = s.Origen;
        bien.Estado = s.Estado;
        bien.Ubicacion = Limpio(s.Ubicacion);
        bien.ResponsableId = s.ResponsableId;
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.Now);
    private NotFoundObjectResult NoEncontrado() => NotFound(ResponseData.Error("No se encontró el bien."));
}

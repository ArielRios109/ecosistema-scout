using Microsoft.EntityFrameworkCore;

namespace EcosistemaScout.Api.Comun;

/// <summary>Parámetros comunes de los listados: página, tamaño y texto de búsqueda (?pagina=1&amp;tamano=20&amp;buscar=...).</summary>
public class ConsultaPaginada
{
    public const int TamanoMaximo = 100;

    private int _pagina = 1;
    private int _tamano = 20;

    /// <summary>Número de página, desde 1.</summary>
    public int Pagina { get => _pagina; set => _pagina = Math.Max(1, value); }

    /// <summary>Elementos por página (entre 1 y 100).</summary>
    public int Tamano { get => _tamano; set => _tamano = Math.Clamp(value, 1, TamanoMaximo); }

    /// <summary>Texto libre para buscar (nombre, RUT, etc., según el listado).</summary>
    public string? Buscar { get; set; }

    /// <summary>Texto de búsqueda normalizado (sin espacios extremos, en minúsculas) o null si viene vacío.</summary>
    public string? BuscarNormalizado => string.IsNullOrWhiteSpace(Buscar) ? null : Buscar.Trim().ToLowerInvariant();
}

/// <summary>Resultado paginado que se entrega dentro de ResponseData.</summary>
public record Pagina<T>(IReadOnlyList<T> Elementos, int Total, int PaginaActual, int Tamano)
{
    public int TotalPaginas => Tamano == 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamano);
}

public static class PaginacionExtensiones
{
    /// <summary>Aplica la paginación a una consulta ya filtrada y ordenada.</summary>
    public static async Task<Pagina<T>> PaginarAsync<T>(this IQueryable<T> consulta, ConsultaPaginada parametros, CancellationToken ct = default)
    {
        var total = await consulta.CountAsync(ct);
        var elementos = await consulta
            .Skip((parametros.Pagina - 1) * parametros.Tamano)
            .Take(parametros.Tamano)
            .ToListAsync(ct);
        return new Pagina<T>(elementos, total, parametros.Pagina, parametros.Tamano);
    }
}

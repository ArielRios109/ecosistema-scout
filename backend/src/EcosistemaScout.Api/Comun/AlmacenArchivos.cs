namespace EcosistemaScout.Api.Comun;

/// <summary>
/// Dónde se guardan los archivos subidos (fotos de bienes, a futuro comprobantes). Hoy es una carpeta del servidor;
/// para pasar a la nube basta con otra implementación de esta interfaz.
/// </summary>
public interface IAlmacenArchivos
{
    /// <summary>Guarda el contenido y devuelve la ruta relativa con que se vuelve a pedir (ej. "bienes/ab12.jpg").</summary>
    Task<string> GuardarAsync(Stream contenido, string carpeta, string extension, CancellationToken ct);

    /// <summary>Abre el archivo para leerlo, o null si ya no existe.</summary>
    Stream? Abrir(string ruta);

    void Eliminar(string ruta);
}

/// <summary>Guarda los archivos en disco, en la carpeta "Archivos:Ruta" (en Docker, un volumen montado).</summary>
public class AlmacenArchivosLocal : IAlmacenArchivos
{
    private readonly string _raiz;

    public AlmacenArchivosLocal(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        _raiz = Path.GetFullPath(configuracion["Archivos:Ruta"] ?? Path.Combine(entorno.ContentRootPath, "archivos"));
        Directory.CreateDirectory(_raiz);
    }

    public async Task<string> GuardarAsync(Stream contenido, string carpeta, string extension, CancellationToken ct)
    {
        // Nombre aleatorio: nunca se usa el nombre que manda el usuario.
        var relativa = $"{carpeta}/{Guid.NewGuid():N}{extension}";
        var completa = RutaSegura(relativa);
        Directory.CreateDirectory(Path.GetDirectoryName(completa)!);
        await using var destino = File.Create(completa);
        await contenido.CopyToAsync(destino, ct);
        return relativa;
    }

    public Stream? Abrir(string ruta)
    {
        var completa = RutaSegura(ruta);
        return File.Exists(completa) ? File.OpenRead(completa) : null;
    }

    public void Eliminar(string ruta)
    {
        var completa = RutaSegura(ruta);
        if (File.Exists(completa)) File.Delete(completa);
    }

    /// <summary>Evita que una ruta guardada apunte fuera de la carpeta de archivos (por ejemplo, con "..").</summary>
    private string RutaSegura(string relativa)
    {
        var completa = Path.GetFullPath(Path.Combine(_raiz, relativa));
        if (!completa.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Ruta de archivo no válida.");
        }
        return completa;
    }
}

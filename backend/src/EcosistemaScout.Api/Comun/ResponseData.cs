namespace EcosistemaScout.Api.Comun;

/// <summary>
/// Formato único de todas las respuestas de la API, exitosas o con error.
/// Se usa junto con el código HTTP correcto (200, 400, 401, 403, 404, 500...).
/// </summary>
/// <typeparam name="T">Tipo de los datos que devuelve la operación.</typeparam>
public record ResponseData<T>(bool Exito, string? Mensaje, T? Datos, IReadOnlyList<string>? Errores);

/// <summary>Atajos para construir respuestas <see cref="ResponseData{T}"/>.</summary>
public static class ResponseData
{
    /// <summary>Respuesta exitosa con datos y, opcionalmente, un mensaje para mostrar al usuario.</summary>
    public static ResponseData<T> Ok<T>(T datos, string? mensaje = null) => new(true, mensaje, datos, null);

    /// <summary>Respuesta de error sin datos.</summary>
    public static ResponseData<object> Error(string mensaje, IReadOnlyList<string>? errores = null) =>
        new(false, mensaje, null, errores);

    /// <summary>Respuesta de error que además incluye datos (por ejemplo, el detalle de un estado).</summary>
    public static ResponseData<T> ErrorConDatos<T>(string mensaje, T datos) => new(false, mensaje, datos, null);
}

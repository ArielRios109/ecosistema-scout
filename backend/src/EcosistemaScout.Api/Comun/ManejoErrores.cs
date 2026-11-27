using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EcosistemaScout.Api.Comun;

/// <summary>
/// Manejo global de errores: toda respuesta de error sale en formato <see cref="ResponseData{T}"/>,
/// con un mensaje claro en español y el código HTTP correspondiente.
/// </summary>
public static class ManejoErrores
{
    /// <summary>Mensajes por defecto para respuestas de error que no traen cuerpo.</summary>
    public static string MensajePorEstado(int estado) => estado switch
    {
        StatusCodes.Status400BadRequest => "La solicitud no es válida.",
        StatusCodes.Status401Unauthorized => "Debes iniciar sesión para continuar.",
        StatusCodes.Status403Forbidden => "No tienes permiso para realizar esta acción.",
        StatusCodes.Status404NotFound => "No se encontró el recurso solicitado.",
        StatusCodes.Status405MethodNotAllowed => "Operación no permitida.",
        StatusCodes.Status409Conflict => "La operación entra en conflicto con datos existentes.",
        _ when estado >= 500 => "Ocurrió un error en el servidor. Intenta nuevamente en unos minutos.",
        _ => "No se pudo completar la solicitud.",
    };

    public static IServiceCollection AgregarManejoErrores(this IServiceCollection services)
    {
        services.AddExceptionHandler<ManejadorExcepciones>();
        services.AddProblemDetails(); // requerido por UseExceptionHandler; el cuerpo lo escribe ManejadorExcepciones

        // Errores de validación de los modelos ([Required], [EmailAddress], etc.)
        services.Configure<ApiBehaviorOptions>(opciones =>
        {
            opciones.InvalidModelStateResponseFactory = contexto =>
            {
                var errores = contexto.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Valor no válido." : e.ErrorMessage)
                    .ToList();
                return new BadRequestObjectResult(
                    ResponseData.Error("Revisa los datos ingresados.", errores));
            };
        });
        return services;
    }

    public static WebApplication UsarManejoErrores(this WebApplication app)
    {
        app.UseExceptionHandler();

        // 401, 403, 404, etc. que no traen cuerpo (por ejemplo, los de autenticación) también usan ResponseData.
        app.UseStatusCodePages(async contexto =>
        {
            var respuesta = contexto.HttpContext.Response;
            await respuesta.WriteAsJsonAsync(ResponseData.Error(MensajePorEstado(respuesta.StatusCode)));
        });
        return app;
    }

    /// <summary>Excepciones no controladas: se registran en el log y se responde 500 sin exponer detalles internos.</summary>
    private sealed class ManejadorExcepciones(ILogger<ManejadorExcepciones> log) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
        {
            log.LogError(excepcion, "Error no controlado en {Ruta}", contexto.Request.Path);
            contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await contexto.Response.WriteAsJsonAsync(
                ResponseData.Error(MensajePorEstado(StatusCodes.Status500InternalServerError)), ct);
            return true;
        }
    }
}

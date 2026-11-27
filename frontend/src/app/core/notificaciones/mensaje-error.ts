import { HttpErrorResponse } from '@angular/common/http';
import { esResponseData } from '../api/response-data';

/**
 * Traduce un error HTTP a un mensaje entendible para el usuario.
 * Si la API envía un ResponseData con `mensaje` (y `errores`), se usa ese texto.
 */
export function mensajeError(error: HttpErrorResponse): string {
  if (error.status !== 0 && esResponseData(error.error) && error.error.mensaje) {
    const detalle = error.error.errores?.length ? ` ${error.error.errores.join(' ')}` : '';
    return error.error.mensaje + detalle;
  }

  switch (error.status) {
    case 0:
      return 'No se pudo conectar con el servidor. Revisa tu conexión e intenta nuevamente.';
    case 400:
      return 'Los datos enviados no son válidos. Revisa el formulario.';
    case 401:
      return 'Tu sesión expiró. Vuelve a iniciar sesión.';
    case 403:
      return 'No tienes permiso para realizar esta acción.';
    case 404:
      return 'No se encontró la información solicitada.';
    case 409:
      return 'La operación entra en conflicto con datos existentes.';
    default:
      return error.status >= 500
        ? 'Ocurrió un error en el servidor. Intenta nuevamente en unos minutos.'
        : 'Ocurrió un error inesperado.';
  }
}

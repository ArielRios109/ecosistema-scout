/** Formato único de las respuestas de la API (backend: Comun/ResponseData.cs). */
export interface ResponseData<T> {
  exito: boolean;
  mensaje: string | null;
  datos: T | null;
  errores: string[] | null;
}

/** Indica si un cuerpo de respuesta tiene el formato ResponseData. */
export function esResponseData(cuerpo: unknown): cuerpo is ResponseData<unknown> {
  return typeof cuerpo === 'object' && cuerpo !== null && 'exito' in cuerpo && 'datos' in cuerpo;
}

import { HttpParams } from '@angular/common/http';

/** Resultado paginado de la API (backend: Comun/Paginacion.cs). */
export interface Pagina<T> {
  elementos: T[];
  total: number;
  paginaActual: number;
  tamano: number;
  totalPaginas: number;
}

/** Parámetros comunes de los listados. */
export interface ConsultaPaginada {
  pagina?: number;
  tamano?: number;
  buscar?: string | null;
}

/** Convierte un objeto de filtros en parámetros de URL, omitiendo los vacíos. */
export function aParametros(filtros: object): HttpParams {
  let parametros = new HttpParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== null && valor !== undefined && valor !== '') {
      parametros = parametros.set(clave, String(valor));
    }
  }
  return parametros;
}

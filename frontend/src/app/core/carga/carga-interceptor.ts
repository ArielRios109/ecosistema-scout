import { HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { Carga } from './carga';

/**
 * Permite que una petición no muestre el spinner global (por ejemplo, verificaciones en segundo plano).
 * Uso: http.get(url, { context: new HttpContext().set(SIN_SPINNER, true) })
 */
export const SIN_SPINNER = new HttpContextToken<boolean>(() => false);

/** Muestra el spinner global mientras haya peticiones a la API en curso (si tardan más de 300 ms). */
export const cargaInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.context.get(SIN_SPINNER) || !req.url.startsWith('/api')) {
    return next(req);
  }

  const carga = inject(Carga);
  carga.iniciar();
  return next(req).pipe(finalize(() => carga.terminar()));
};

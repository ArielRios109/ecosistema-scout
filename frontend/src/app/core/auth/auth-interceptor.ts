import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { mensajeError } from '../notificaciones/mensaje-error';
import { Notificaciones } from '../notificaciones/notificaciones';
import { Auth } from './auth';

/**
 * Permite que una petición maneje sus propios errores sin el snackbar automático.
 * Uso: http.get(url, { context: new HttpContext().set(SIN_NOTIFICACION_ERROR, true) })
 */
export const SIN_NOTIFICACION_ERROR = new HttpContextToken<boolean>(() => false);

/**
 * Agrega el token JWT a las peticiones a /api, muestra los errores con el snackbar
 * común y cierra la sesión si el servidor responde 401.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(Auth);
  const notificaciones = inject(Notificaciones);
  const token = auth.token;

  const peticion =
    token && req.url.startsWith('/api')
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(peticion).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (!req.context.get(SIN_NOTIFICACION_ERROR)) {
          notificaciones.error(mensajeError(error));
        }
        const esLogin = req.url.endsWith('/api/auth/login');
        if (error.status === 401 && token && !esLogin) {
          auth.logout();
        }
      }
      return throwError(() => error);
    }),
  );
};

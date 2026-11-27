import { HttpContextToken, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { map } from 'rxjs';
import { Notificaciones } from '../notificaciones/notificaciones';
import { esResponseData } from './response-data';

/**
 * Muestra en el snackbar el mensaje de éxito que envía la API (por ejemplo, "Bienvenido...").
 * Uso: http.post(url, cuerpo, { context: new HttpContext().set(MOSTRAR_MENSAJE_EXITO, true) })
 */
export const MOSTRAR_MENSAJE_EXITO = new HttpContextToken<boolean>(() => false);

/**
 * "Desenvuelve" las respuestas ResponseData de la API: cada pantalla recibe directamente `datos`.
 * Los errores no pasan por aquí: los maneja el interceptor de autenticación con el snackbar común.
 */
export const respuestaInterceptor: HttpInterceptorFn = (req, next) => {
  const notificaciones = inject(Notificaciones);

  return next(req).pipe(
    map((evento) => {
      if (!(evento instanceof HttpResponse) || !esResponseData(evento.body)) {
        return evento;
      }
      if (req.context.get(MOSTRAR_MENSAJE_EXITO) && evento.body.mensaje) {
        notificaciones.exito(evento.body.mensaje);
      }
      return evento.clone({ body: evento.body.datos });
    }),
  );
};

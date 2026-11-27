import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { Notificaciones } from '../notificaciones/notificaciones';
import { Auth } from './auth';
import { Modulo, puedeVerModulo } from './permisos';

/** Solo deja entrar a las rutas protegidas si hay una sesión iniciada. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  return auth.autenticado() ? true : inject(Router).createUrlTree(['/login']);
};

/** Evita mostrar la pantalla de login si ya hay una sesión iniciada. */
export const invitadoGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  return auth.autenticado() ? inject(Router).createUrlTree(['/']) : true;
};

/**
 * Con una contraseña temporal, la única pantalla disponible es "Cambiar mi contraseña"
 * (la API también bloquea el resto de las peticiones).
 */
export const cambioPasswordGuard: CanActivateChildFn = (_ruta, estado) => {
  const auth = inject(Auth);
  if (!auth.debeCambiarPassword() || estado.url.startsWith('/cambiar-password')) {
    return true;
  }
  return inject(Router).createUrlTree(['/cambiar-password']);
};

/**
 * Permite entrar a un módulo solo si el rol del usuario lo autoriza.
 * El módulo se indica en la ruta: data: { modulo: 'finanzas' }.
 * Si no tiene permiso, vuelve a Inicio y lo avisa con el snackbar.
 */
export const rolGuard: CanActivateFn = (ruta) => {
  const modulo = ruta.data['modulo'] as Modulo;
  const roles = inject(Auth).usuario()?.roles ?? [];

  if (puedeVerModulo(roles, modulo)) {
    return true;
  }

  inject(Notificaciones).aviso('No tienes permiso para acceder a ese módulo.');
  return inject(Router).createUrlTree(['/inicio']);
};

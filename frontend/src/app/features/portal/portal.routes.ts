import { Routes } from '@angular/router';
import { EnConstruccion } from '../../shared/en-construccion/en-construccion';

/** Rutas del módulo Portal de pagos (se carga solo cuando se visita). */
export const PORTAL_ROUTES: Routes = [
  {
    path: '',
    component: EnConstruccion,
    data: { titulo: 'Portal de pagos', icono: 'payments' },
  },
];

import { Routes } from '@angular/router';
import { EnConstruccion } from '../../shared/en-construccion/en-construccion';

/** Rutas del módulo Gestión de Miembros (se carga solo cuando se visita). */
export const MIEMBROS_ROUTES: Routes = [
  {
    path: '',
    component: EnConstruccion,
    data: { titulo: 'Miembros', icono: 'groups' },
  },
];

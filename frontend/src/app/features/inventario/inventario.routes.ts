import { Routes } from '@angular/router';
import { EnConstruccion } from '../../shared/en-construccion/en-construccion';

/** Rutas del módulo Inventario (se carga solo cuando se visita). */
export const INVENTARIO_ROUTES: Routes = [
  {
    path: '',
    component: EnConstruccion,
    data: { titulo: 'Inventario', icono: 'inventory_2' },
  },
];

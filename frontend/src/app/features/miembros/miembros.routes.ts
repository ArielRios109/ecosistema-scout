import { Routes } from '@angular/router';
import { EnConstruccion } from '../../shared/en-construccion/en-construccion';
import { MiembrosLayout } from './miembros-layout';

/** Rutas del módulo Gestión de Miembros (se carga solo cuando se visita). */
export const MIEMBROS_ROUTES: Routes = [
  {
    path: '',
    component: MiembrosLayout,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'beneficiarios' },
      {
        path: 'beneficiarios',
        component: EnConstruccion,
        data: { titulo: 'Beneficiarios', icono: 'groups' },
        title: 'Beneficiarios · Talí',
      },
      {
        path: 'unidades',
        loadComponent: () => import('./unidades/unidades').then((m) => m.Unidades),
        title: 'Unidades y patrullas · Talí',
      },
      {
        path: 'apoderados',
        component: EnConstruccion,
        data: { titulo: 'Apoderados', icono: 'family_restroom' },
        title: 'Apoderados · Talí',
      },
      {
        path: 'voluntarios',
        component: EnConstruccion,
        data: { titulo: 'Voluntarios', icono: 'volunteer_activism' },
        title: 'Voluntarios · Talí',
      },
    ],
  },
];

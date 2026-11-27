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
        loadComponent: () => import('./beneficiarios/beneficiarios').then((m) => m.Beneficiarios),
        title: 'Beneficiarios · Talí',
      },
      {
        path: 'beneficiarios/:id',
        loadComponent: () => import('./beneficiarios/beneficiario-detalle').then((m) => m.BeneficiarioDetalle),
        title: 'Ficha del beneficiario · Talí',
      },
      {
        path: 'unidades',
        loadComponent: () => import('./unidades/unidades').then((m) => m.Unidades),
        title: 'Unidades y patrullas · Talí',
      },
      {
        path: 'apoderados',
        loadComponent: () => import('./apoderados/apoderados').then((m) => m.Apoderados),
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

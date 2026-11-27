import { Routes } from '@angular/router';
import { cambiosSinGuardarGuard } from '../../shared/confirmacion/cambios-sin-guardar.guard';
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
        path: 'beneficiarios/nuevo',
        loadComponent: () => import('./beneficiarios/inscripcion').then((m) => m.Inscripcion),
        canDeactivate: [cambiosSinGuardarGuard],
        title: 'Inscribir beneficiario · Talí',
      },
      {
        path: 'beneficiarios/:id/editar',
        loadComponent: () =>
          import('./beneficiarios/editar-beneficiario').then((m) => m.EditarBeneficiario),
        canDeactivate: [cambiosSinGuardarGuard],
        title: 'Editar beneficiario · Talí',
      },
      {
        path: 'beneficiarios/:id',
        loadComponent: () =>
          import('./beneficiarios/beneficiario-detalle').then((m) => m.BeneficiarioDetalle),
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
        loadComponent: () => import('./voluntarios/voluntarios').then((m) => m.Voluntarios),
        title: 'Voluntarios · Talí',
      },
    ],
  },
];

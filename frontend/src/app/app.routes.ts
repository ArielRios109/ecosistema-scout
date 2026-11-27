import { Routes } from '@angular/router';
import { authGuard, invitadoGuard } from './core/auth/auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
    canActivate: [invitadoGuard],
    title: 'Ingresar · Ecosistema Scout',
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      {
        path: 'inicio',
        loadComponent: () => import('./features/inicio/inicio').then((m) => m.Inicio),
        title: 'Inicio · Ecosistema Scout',
      },
      {
        path: 'miembros',
        loadChildren: () => import('./features/miembros/miembros.routes').then((m) => m.MIEMBROS_ROUTES),
        title: 'Miembros · Ecosistema Scout',
      },
      {
        path: 'inventario',
        loadChildren: () => import('./features/inventario/inventario.routes').then((m) => m.INVENTARIO_ROUTES),
        title: 'Inventario · Ecosistema Scout',
      },
      {
        path: 'finanzas',
        loadChildren: () => import('./features/finanzas/finanzas.routes').then((m) => m.FINANZAS_ROUTES),
        title: 'Finanzas · Ecosistema Scout',
      },
      {
        path: 'portal',
        loadChildren: () => import('./features/portal/portal.routes').then((m) => m.PORTAL_ROUTES),
        title: 'Portal de pagos · Ecosistema Scout',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

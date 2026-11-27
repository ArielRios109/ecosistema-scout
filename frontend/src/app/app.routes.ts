import { Routes } from '@angular/router';
import { authGuard, cambioPasswordGuard, invitadoGuard, rolGuard } from './core/auth/auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
    canActivate: [invitadoGuard],
    title: 'Ingresar · Talí',
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    canActivateChild: [cambioPasswordGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      {
        path: 'inicio',
        loadComponent: () => import('./features/inicio/inicio').then((m) => m.Inicio),
        title: 'Inicio · Talí',
      },
      {
        path: 'miembros',
        canActivate: [rolGuard],
        data: { modulo: 'miembros' },
        loadChildren: () => import('./features/miembros/miembros.routes').then((m) => m.MIEMBROS_ROUTES),
        title: 'Miembros · Talí',
      },
      {
        path: 'inventario',
        canActivate: [rolGuard],
        data: { modulo: 'inventario' },
        loadChildren: () => import('./features/inventario/inventario.routes').then((m) => m.INVENTARIO_ROUTES),
        title: 'Inventario · Talí',
      },
      {
        path: 'finanzas',
        canActivate: [rolGuard],
        data: { modulo: 'finanzas' },
        loadChildren: () => import('./features/finanzas/finanzas.routes').then((m) => m.FINANZAS_ROUTES),
        title: 'Finanzas · Talí',
      },
      {
        path: 'portal',
        canActivate: [rolGuard],
        data: { modulo: 'portal' },
        loadChildren: () => import('./features/portal/portal.routes').then((m) => m.PORTAL_ROUTES),
        title: 'Portal de pagos · Talí',
      },
      {
        path: 'usuarios',
        canActivate: [rolGuard],
        data: { modulo: 'administracion' },
        loadComponent: () => import('./features/usuarios/usuarios').then((m) => m.Usuarios),
        title: 'Usuarios · Talí',
      },
      {
        path: 'cambiar-password',
        loadComponent: () => import('./features/cuenta/cambiar-password').then((m) => m.CambiarPassword),
        title: 'Cambiar mi contraseña · Talí',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

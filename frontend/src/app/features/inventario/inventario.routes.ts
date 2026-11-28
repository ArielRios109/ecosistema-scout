import { Routes } from '@angular/router';
import { cambiosSinGuardarGuard } from '../../shared/confirmacion/cambios-sin-guardar.guard';
import { InventarioLayout } from './inventario-layout';

/** Rutas del módulo Inventario (se carga solo cuando se visita). */
export const INVENTARIO_ROUTES: Routes = [
  {
    path: '',
    component: InventarioLayout,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'bienes' },
      {
        path: 'bienes',
        loadComponent: () => import('./bienes/bienes').then((m) => m.Bienes),
        title: 'Bienes · Talí',
      },
      {
        path: 'bienes/nuevo',
        loadComponent: () => import('./bienes/bien-formulario').then((m) => m.BienFormulario),
        canDeactivate: [cambiosSinGuardarGuard],
        title: 'Nuevo bien · Talí',
      },
      {
        path: 'bienes/:id/editar',
        loadComponent: () => import('./bienes/bien-formulario').then((m) => m.BienFormulario),
        canDeactivate: [cambiosSinGuardarGuard],
        title: 'Editar bien · Talí',
      },
      {
        path: 'bienes/:id',
        loadComponent: () => import('./bienes/bien-detalle').then((m) => m.BienDetalleVista),
        title: 'Ficha del bien · Talí',
      },
      {
        path: 'prestamos',
        loadComponent: () => import('./prestamos/prestamos').then((m) => m.Prestamos),
        title: 'Préstamos · Talí',
      },
      {
        path: 'categorias',
        loadComponent: () => import('./categorias/categorias').then((m) => m.Categorias),
        title: 'Categorías · Talí',
      },
    ],
  },
];

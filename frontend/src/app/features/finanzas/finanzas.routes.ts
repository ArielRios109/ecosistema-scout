import { Routes } from '@angular/router';
import { EnConstruccion } from '../../shared/en-construccion/en-construccion';

/** Rutas del módulo Financiamiento (se carga solo cuando se visita). */
export const FINANZAS_ROUTES: Routes = [
  {
    path: '',
    component: EnConstruccion,
    data: { titulo: 'Finanzas', icono: 'account_balance_wallet' },
  },
];

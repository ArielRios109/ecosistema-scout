import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { ConfirmacionService } from './confirmacion';

/** Página con un formulario que puede quedar a medio llenar. */
export interface ConCambiosSinGuardar {
  tieneCambiosSinGuardar(): boolean;
}

/** Antes de salir de la página, pide confirmar si hay cambios sin guardar. Uso: canDeactivate: [cambiosSinGuardarGuard]. */
export const cambiosSinGuardarGuard: CanDeactivateFn<ConCambiosSinGuardar> = (componente) =>
  !componente?.tieneCambiosSinGuardar() || inject(ConfirmacionService).descartarCambios();

import { inject, Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

type TipoNotificacion = 'error' | 'exito' | 'aviso';

const DURACION_MS: Record<TipoNotificacion, number> = {
  error: 6000,
  aviso: 5000,
  exito: 3500,
};

/**
 * Servicio único para mostrar mensajes al usuario.
 * Convención del proyecto: todos los errores se muestran con un snackbar de
 * Angular Material 3 centrado en la parte superior (ver docs/convenciones.md).
 */
@Injectable({ providedIn: 'root' })
export class Notificaciones {
  private readonly snackBar = inject(MatSnackBar);

  error(mensaje: string): void {
    this.mostrar(mensaje, 'error');
  }

  exito(mensaje: string): void {
    this.mostrar(mensaje, 'exito');
  }

  aviso(mensaje: string): void {
    this.mostrar(mensaje, 'aviso');
  }

  private mostrar(mensaje: string, tipo: TipoNotificacion): void {
    const config: MatSnackBarConfig = {
      duration: DURACION_MS[tipo],
      horizontalPosition: 'center',
      verticalPosition: 'top',
      panelClass: ['notificacion', `notificacion-${tipo}`],
      politeness: tipo === 'error' ? 'assertive' : 'polite',
    };
    this.snackBar.open(mensaje, 'Cerrar', config);
  }
}

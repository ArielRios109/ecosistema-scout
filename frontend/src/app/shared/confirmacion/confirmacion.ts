import { Component, inject, Injectable } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { concatMap, filter, map, Observable } from 'rxjs';

/** Variantes del diálogo; cada una trae su icono, título y botón por defecto. */
export type TipoConfirmacion = 'eliminar' | 'descartar' | 'guardar' | 'generica';

export interface OpcionesConfirmacion {
  tipo?: TipoConfirmacion;
  titulo?: string;
  mensaje: string;
  /** Texto del botón que confirma. */
  confirmar?: string;
  /** Texto del botón que cancela. */
  cancelar?: string;
  icono?: string;
  /** Pinta el botón de confirmar con el color de error (por defecto, solo en "eliminar" y "descartar"). */
  peligroso?: boolean;
}

const PREDEFINIDAS: Record<
  TipoConfirmacion,
  Required<Omit<OpcionesConfirmacion, 'mensaje' | 'tipo'>>
> = {
  eliminar: {
    titulo: 'Eliminar',
    confirmar: 'Eliminar',
    cancelar: 'Cancelar',
    icono: 'delete',
    peligroso: true,
  },
  descartar: {
    titulo: '¿Descartar los cambios?',
    confirmar: 'Descartar',
    cancelar: 'Seguir editando',
    icono: 'edit_off',
    peligroso: true,
  },
  guardar: {
    titulo: '¿Guardar los cambios?',
    confirmar: 'Guardar',
    cancelar: 'Cancelar',
    icono: 'save',
    peligroso: false,
  },
  generica: {
    titulo: 'Confirmar',
    confirmar: 'Confirmar',
    cancelar: 'Cancelar',
    icono: 'help',
    peligroso: false,
  },
};

/** Une las opciones con las de la variante elegida. */
export function completarOpciones(opciones: OpcionesConfirmacion) {
  const base = PREDEFINIDAS[opciones.tipo ?? 'generica'];
  return {
    titulo: opciones.titulo ?? base.titulo,
    mensaje: opciones.mensaje,
    confirmar: opciones.confirmar ?? base.confirmar,
    cancelar: opciones.cancelar ?? base.cancelar,
    icono: opciones.icono ?? base.icono,
    peligroso: opciones.peligroso ?? base.peligroso,
  };
}

/** Diálogo único de confirmación de la aplicación (Material 3). Se cierra con true si la persona confirma. */
@Component({
  selector: 'app-confirmacion-dialogo',
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <mat-icon class="icono" [class.peligroso]="datos.peligroso">{{ datos.icono }}</mat-icon>
    <h2 mat-dialog-title class="texto-centro">{{ datos.titulo }}</h2>
    <mat-dialog-content class="texto-centro">{{ datos.mensaje }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">{{ datos.cancelar }}</button>
      <button
        mat-flat-button
        [class.boton-peligro]="datos.peligroso"
        [mat-dialog-close]="true"
        cdkFocusInitial
      >
        {{ datos.confirmar }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    :host {
      display: block;
      padding-top: 24px;
    }
    .icono {
      display: block;
      margin: 0 auto;
      color: var(--mat-sys-secondary);
      &.peligroso {
        color: var(--mat-sys-error);
      }
    }
    .boton-peligro {
      background-color: var(--mat-sys-error);
      color: var(--mat-sys-on-error);
    }
  `,
})
export class ConfirmacionDialogo {
  protected readonly datos = inject<ReturnType<typeof completarOpciones>>(MAT_DIALOG_DATA);
}

/**
 * Servicio único para pedir confirmación. Todas las variantes devuelven un Observable<boolean>.
 *
 * Con `ejecutar`, la acción (normalmente una petición a la API) solo corre si la persona confirma. El resultado
 * llega en ResponseData y lo muestra el snackbar global: el mensaje de éxito si la petición usa
 * MOSTRAR_MENSAJE_EXITO, y el mensaje y los errores si falla (interceptores de core/).
 */
@Injectable({ providedIn: 'root' })
export class ConfirmacionService {
  private readonly dialogo = inject(MatDialog);

  confirmar(opciones: OpcionesConfirmacion): Observable<boolean> {
    return this.dialogo
      .open(ConfirmacionDialogo, {
        data: completarOpciones(opciones),
        width: '400px',
        autoFocus: 'dialog',
      })
      .afterClosed()
      .pipe(map((respuesta) => respuesta === true));
  }

  /** "¿Eliminar X?" con botón de peligro. */
  eliminar(elemento: string, detalle = 'Esta acción no se puede deshacer.'): Observable<boolean> {
    return this.confirmar({ tipo: 'eliminar', titulo: `¿Eliminar ${elemento}?`, mensaje: detalle });
  }

  /** "¿Descartar los cambios?" al cerrar un formulario o salir de una página con cambios sin guardar. */
  descartarCambios(): Observable<boolean> {
    return this.confirmar({
      tipo: 'descartar',
      mensaje: 'Hay cambios sin guardar. Si sales ahora, se perderán.',
    });
  }

  /** "¿Guardar los cambios?" para guardados con consecuencias (por ejemplo, cambiar de unidad). */
  guardar(mensaje: string): Observable<boolean> {
    return this.confirmar({ tipo: 'guardar', mensaje });
  }

  /** Pide confirmación y, solo si la persona acepta, ejecuta la acción. Emite el resultado de la acción. */
  ejecutar<T>(opciones: OpcionesConfirmacion, accion: () => Observable<T>): Observable<T> {
    return this.confirmar(opciones).pipe(
      filter(Boolean),
      concatMap(() => accion()),
    );
  }
}

/**
 * Protege un diálogo con formulario: Esc, el clic fuera o "Cancelar" piden confirmar si hay cambios sin guardar.
 * Se llama al crear el componente del diálogo (contexto de inyección) y devuelve la función para "Cancelar".
 */
export function protegerDialogo(
  ref: MatDialogRef<unknown>,
  formulario: AbstractControl,
): () => void {
  const confirmacion = inject(ConfirmacionService);
  ref.disableClose = true;
  const intentarCerrar = () => {
    if (!formulario.dirty) {
      ref.close();
      return;
    }
    confirmacion.descartarCambios().subscribe((descartar) => descartar && ref.close());
  };
  ref.backdropClick().subscribe(intentarCerrar);
  ref
    .keydownEvents()
    .pipe(filter((evento) => evento.key === 'Escape'))
    .subscribe((evento) => {
      evento.preventDefault();
      intentarCerrar();
    });
  return intentarCerrar;
}

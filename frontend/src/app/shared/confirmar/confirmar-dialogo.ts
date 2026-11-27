import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { Observable } from 'rxjs';

export interface DatosConfirmacion {
  titulo: string;
  mensaje: string;
  confirmar?: string;
  peligroso?: boolean;
}

/** Diálogo de confirmación reutilizable (Material 3). Devuelve true si la persona confirma. */
@Component({
  selector: 'app-confirmar-dialogo',
  imports: [MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>{{ datos.titulo }}</h2>
    <mat-dialog-content>{{ datos.mensaje }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">Cancelar</button>
      <button mat-flat-button [class.peligroso]="datos.peligroso" [mat-dialog-close]="true">
        {{ datos.confirmar ?? 'Confirmar' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .peligroso {
      background-color: var(--mat-sys-error);
      color: var(--mat-sys-on-error);
    }
  `,
})
export class ConfirmarDialogo {
  protected readonly datos = inject<DatosConfirmacion>(MAT_DIALOG_DATA);
}

/** Abre el diálogo de confirmación. */
export function confirmar(dialogo: MatDialog, datos: DatosConfirmacion): Observable<boolean | undefined> {
  return dialogo.open(ConfirmarDialogo, { data: datos, width: '420px' }).afterClosed();
}

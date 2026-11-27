import { Clipboard } from '@angular/cdk/clipboard';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Notificaciones } from '../../core/notificaciones/notificaciones';
import { UsuarioConPasswordTemporal } from './usuarios.service';

/** Muestra una sola vez la contraseña temporal para que el administrador la comunique a la persona. */
@Component({
  selector: 'app-password-temporal-dialogo',
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>Contraseña temporal</h2>
    <mat-dialog-content>
      <p class="m-0">
        Comparte esta contraseña con <strong>{{ datos.usuario.nombre }}</strong> ({{
          datos.usuario.email
        }}). Al ingresar, el sistema le pedirá crear su propia contraseña.
      </p>
      <div class="clave flex items-center justify-between gap-2 mt-3 p-3">
        <code>{{ datos.passwordTemporal }}</code>
        <button mat-icon-button type="button" (click)="copiar()" aria-label="Copiar contraseña">
          <mat-icon>content_copy</mat-icon>
        </button>
      </div>
      <p class="texto-secundario mt-2 m-0">Por seguridad, no se volverá a mostrar.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button mat-dialog-close>Listo</button>
    </mat-dialog-actions>
  `,
  styles: `
    .clave {
      border-radius: 12px;
      background: var(--mat-sys-surface-container-high);
    }
    code {
      font: var(--mat-sys-title-large);
      letter-spacing: 0.08em;
    }
  `,
})
export class PasswordTemporalDialogo {
  protected readonly datos = inject<UsuarioConPasswordTemporal>(MAT_DIALOG_DATA);
  private readonly clipboard = inject(Clipboard);
  private readonly notificaciones = inject(Notificaciones);

  protected copiar(): void {
    if (this.clipboard.copy(this.datos.passwordTemporal)) {
      this.notificaciones.exito('Contraseña copiada.');
    }
  }
}

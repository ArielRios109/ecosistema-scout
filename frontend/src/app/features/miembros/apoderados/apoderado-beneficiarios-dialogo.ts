import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import { ApoderadoDetalle } from '../miembros.models';

/** Beneficiarios a cargo de un apoderado, con acceso directo a su ficha. */
@Component({
  selector: 'app-apoderado-beneficiarios-dialogo',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatListModule, RouterLink],
  template: `
    <h2 mat-dialog-title>{{ datos.nombres }} {{ datos.apellidos }}</h2>
    <mat-dialog-content>
      @if (datos.beneficiarios.length === 0) {
        <p class="texto-secundario m-0">No tiene beneficiarios vinculados.</p>
      } @else {
        <mat-nav-list>
          @for (b of datos.beneficiarios; track b.beneficiarioId) {
            <a
              mat-list-item
              [routerLink]="['/miembros/beneficiarios', b.beneficiarioId]"
              mat-dialog-close
            >
              <mat-icon matListItemIcon>{{ b.esPrincipal ? 'star' : 'person' }}</mat-icon>
              <span matListItemTitle>{{ b.nombre }}</span>
              <span matListItemLine>
                {{ b.parentesco }}{{ b.esPrincipal ? ' · Principal' : '' }} · {{ b.unidad
                }}{{ b.estado === 'retirado' ? ' · Retirado' : '' }}
              </span>
            </a>
          }
        </mat-nav-list>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cerrar</button>
    </mat-dialog-actions>
  `,
})
export class ApoderadoBeneficiariosDialogo {
  protected readonly datos = inject<ApoderadoDetalle>(MAT_DIALOG_DATA);
}

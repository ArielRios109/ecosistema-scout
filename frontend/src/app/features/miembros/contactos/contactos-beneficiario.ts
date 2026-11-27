import { Component, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatTooltipModule } from '@angular/material/tooltip';
import { concatMap, filter } from 'rxjs';
import { confirmar } from '../../../shared/confirmar/confirmar-dialogo';
import { ContactoEmergencia, SolicitudContacto } from '../miembros.models';
import { ContactosService } from '../miembros.service';
import { ContactoDialogo } from './contacto-dialogo';

/** Pestaña "Contactos" de la ficha del beneficiario: a quién llamar en caso de emergencia. */
@Component({
  selector: 'app-contactos-beneficiario',
  imports: [MatButtonModule, MatIconModule, MatListModule, MatTooltipModule],
  template: `
    <div class="flex items-center justify-between gap-2 mt-3 mb-2">
      <p class="texto-secundario m-0">Hasta {{ maximo }} personas a quienes llamar si ocurre algo en una actividad.</p>
      <button mat-stroked-button (click)="agregar()" [disabled]="contactos().length >= maximo">
        <mat-icon>add_call</mat-icon>
        Agregar contacto
      </button>
    </div>

    @if (contactos().length === 0) {
      <p class="texto-secundario">Aún no tiene contactos de emergencia.</p>
    } @else {
      <mat-list class="lista">
        @for (c of contactos(); track c.id) {
          <mat-list-item>
            <mat-icon matListItemIcon>contact_phone</mat-icon>
            <span matListItemTitle>{{ c.nombre }}{{ c.parentesco ? ' · ' + c.parentesco : '' }}</span>
            <a matListItemLine [href]="'tel:' + c.telefono">{{ c.telefono }}</a>
            <div matListItemMeta class="flex">
              <button mat-icon-button (click)="editar(c)" matTooltip="Editar" aria-label="Editar contacto">
                <mat-icon>edit</mat-icon>
              </button>
              <button mat-icon-button (click)="eliminar(c)" matTooltip="Eliminar" aria-label="Eliminar contacto">
                <mat-icon>delete</mat-icon>
              </button>
            </div>
          </mat-list-item>
        }
      </mat-list>
    }
  `,
  styles: `
    .lista {
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px;
      max-width: 640px;
    }
    a {
      color: var(--mat-sys-primary);
    }
  `,
})
export class ContactosBeneficiario {
  readonly beneficiarioId = input.required<number>();

  private readonly servicio = inject(ContactosService);
  private readonly dialogo = inject(MatDialog);
  protected readonly contactos = signal<ContactoEmergencia[]>([]);
  protected readonly maximo = 5;

  constructor() {
    effect(() => this.servicio.listar(this.beneficiarioId()).subscribe((c) => this.contactos.set(c)));
  }

  protected agregar(): void {
    this.formulario()
      .pipe(concatMap((datos) => this.servicio.crear(this.beneficiarioId(), datos)))
      .subscribe((c) => this.contactos.set(c));
  }

  protected editar(contacto: ContactoEmergencia): void {
    this.formulario(contacto)
      .pipe(concatMap((datos) => this.servicio.editar(this.beneficiarioId(), contacto.id, datos)))
      .subscribe((c) => this.contactos.set(c));
  }

  protected eliminar(contacto: ContactoEmergencia): void {
    confirmar(this.dialogo, {
      titulo: 'Eliminar contacto',
      mensaje: `Se eliminará a ${contacto.nombre} de los contactos de emergencia.`,
      confirmar: 'Eliminar',
      peligroso: true,
    })
      .pipe(
        filter(Boolean),
        concatMap(() => this.servicio.eliminar(this.beneficiarioId(), contacto.id)),
      )
      .subscribe((c) => this.contactos.set(c));
  }

  private formulario(contacto?: ContactoEmergencia) {
    return this.dialogo
      .open<ContactoDialogo, ContactoEmergencia | undefined, SolicitudContacto>(ContactoDialogo, {
        data: contacto,
        width: '440px',
        maxWidth: '95vw',
      })
      .afterClosed()
      .pipe(filter((datos): datos is SolicitudContacto => !!datos));
  }
}

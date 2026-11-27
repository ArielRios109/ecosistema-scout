import { Component, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { concatMap, filter, map, Observable, of } from 'rxjs';
import { ConfirmacionService } from '../../../shared/confirmacion/confirmacion';
import { VinculoApoderado } from '../miembros.models';
import { ApoderadosService } from '../miembros.service';
import {
  ApoderadoDialogo,
  DatosApoderadoDialogo,
  ResultadoApoderadoDialogo,
} from './apoderado-dialogo';
import { DatosVinculoDialogo, ResultadoVinculo, VinculoDialogo } from './vinculo-dialogo';

/** Pestaña "Apoderados" de la ficha del beneficiario: vincular, cambiar parentesco o principal y desvincular. */
@Component({
  selector: 'app-apoderados-beneficiario',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatMenuModule],
  template: `
    <div class="flex items-center justify-between gap-2 mt-3 mb-3">
      <p class="texto-secundario m-0">Personas responsables de {{ nombre() }} ante el grupo.</p>
      <button mat-stroked-button (click)="vincular()">
        <mat-icon>add_link</mat-icon>
        Vincular apoderado
      </button>
    </div>

    <div class="grilla">
      @for (v of vinculos(); track v.apoderadoId) {
        <mat-card appearance="outlined">
          <mat-card-header>
            <mat-icon matCardAvatar class="avatar">{{
              v.esPrincipal ? 'star' : 'person'
            }}</mat-icon>
            <mat-card-title>{{ v.nombre }}</mat-card-title>
            <mat-card-subtitle
              >{{ v.parentesco }}{{ v.esPrincipal ? ' · Principal' : '' }}</mat-card-subtitle
            >
            <button
              mat-icon-button
              class="menu"
              [matMenuTriggerFor]="menu"
              aria-label="Acciones del vínculo"
            >
              <mat-icon>more_vert</mat-icon>
            </button>
            <mat-menu #menu="matMenu">
              <button mat-menu-item (click)="editar(v)">
                <mat-icon>edit</mat-icon>
                <span>Editar parentesco</span>
              </button>
              <button mat-menu-item (click)="desvincular(v)">
                <mat-icon>link_off</mat-icon>
                <span>Desvincular</span>
              </button>
            </mat-menu>
          </mat-card-header>
          <mat-card-content class="flex-col gap-1 mt-2">
            <span class="flex items-center gap-1"
              ><mat-icon class="icono">badge</mat-icon>{{ v.rut }}</span
            >
            <span class="flex items-center gap-1"
              ><mat-icon class="icono">mail</mat-icon>{{ v.email }}</span
            >
            @if (v.telefono) {
              <span class="flex items-center gap-1"
                ><mat-icon class="icono">call</mat-icon>{{ v.telefono }}</span
              >
            }
            <span class="flex items-center gap-1 texto-secundario">
              <mat-icon class="icono">{{
                v.tieneCuenta ? 'verified_user' : 'no_accounts'
              }}</mat-icon>
              {{ v.tieneCuenta ? 'Con cuenta del Portal' : 'Sin cuenta del Portal' }}
            </span>
          </mat-card-content>
        </mat-card>
      } @empty {
        <p class="texto-secundario">Aún no tiene apoderados vinculados.</p>
      }
    </div>
  `,
  styles: `
    .grilla {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 16px;
    }
    mat-card-header {
      position: relative;
      padding-right: 48px;
    }
    .menu {
      position: absolute;
      top: 4px;
      right: 4px;
    }
    .avatar {
      display: grid;
      place-items: center;
      background-color: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
    }
    .icono {
      width: 18px;
      height: 18px;
      font-size: 18px;
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class ApoderadosBeneficiario {
  readonly beneficiarioId = input.required<number>();
  readonly nombre = input.required<string>();

  private readonly servicio = inject(ApoderadosService);
  private readonly dialogo = inject(MatDialog);
  private readonly confirmacion = inject(ConfirmacionService);
  protected readonly vinculos = signal<VinculoApoderado[]>([]);

  constructor() {
    effect(() =>
      this.servicio.vinculos(this.beneficiarioId()).subscribe((v) => this.vinculos.set(v)),
    );
  }

  protected vincular(): void {
    this.abrir({ nombreBeneficiario: this.nombre() })
      .pipe(
        concatMap((resultado) =>
          this.apoderadoDe(resultado).pipe(map((apoderadoId) => ({ ...resultado, apoderadoId }))),
        ),
        concatMap((datos) => this.servicio.vincular(this.beneficiarioId(), datos)),
      )
      .subscribe((v) => this.vinculos.set(v));
  }

  protected editar(vinculo: VinculoApoderado): void {
    this.abrir({ nombreBeneficiario: this.nombre(), vinculo })
      .pipe(
        concatMap(({ parentesco, esPrincipal }) =>
          this.servicio.editarVinculo(this.beneficiarioId(), vinculo.apoderadoId, {
            parentesco,
            esPrincipal,
          }),
        ),
      )
      .subscribe((v) => this.vinculos.set(v));
  }

  protected desvincular(vinculo: VinculoApoderado): void {
    this.confirmacion
      .ejecutar(
        {
          titulo: 'Desvincular apoderado',
          mensaje: `${vinculo.nombre} dejará de ser apoderado de ${this.nombre()}. Sus datos se conservan.`,
          confirmar: 'Desvincular',
          peligroso: true,
        },
        () => this.servicio.desvincular(this.beneficiarioId(), vinculo.apoderadoId),
      )
      .subscribe((v) => this.vinculos.set(v));
  }

  private abrir(datos: DatosVinculoDialogo): Observable<ResultadoVinculo> {
    return this.dialogo
      .open<VinculoDialogo, DatosVinculoDialogo, ResultadoVinculo>(VinculoDialogo, {
        data: datos,
        width: '480px',
      })
      .afterClosed()
      .pipe(filter((r): r is ResultadoVinculo => !!r));
  }

  /** Devuelve el id elegido o, si se pidió uno nuevo, abre el formulario y lo registra. */
  private apoderadoDe(resultado: ResultadoVinculo): Observable<number> {
    if (resultado.apoderadoId !== 'nuevo') return of(resultado.apoderadoId);
    return this.dialogo
      .open<ApoderadoDialogo, DatosApoderadoDialogo, ResultadoApoderadoDialogo>(ApoderadoDialogo, {
        data: { permitirExistente: true },
        width: '520px',
      })
      .afterClosed()
      .pipe(
        filter((r): r is ResultadoApoderadoDialogo => !!r),
        // Si el RUT ya existía y se eligió usar a esa persona, se vincula sin crear otra.
        concatMap((r) =>
          r.tipo === 'existente'
            ? of(r.id)
            : this.servicio.crear(r.datos).pipe(map((creado) => creado.id)),
        ),
      );
  }
}

import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroupDirective, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { concatMap, filter, startWith } from 'rxjs';
import { aFechaIso, formatearFecha, proveerFechas } from '../../../core/i18n/fechas';
import { confirmar } from '../../../shared/confirmar/confirmar-dialogo';
import { Cargo, CARGOS_SUGERIDOS, Unidad, VoluntarioDetalle } from '../miembros.models';
import { UnidadesService, VoluntariosService } from '../miembros.service';

/** Cargos de un voluntario: los vigentes y su historial, asignar uno nuevo, terminarlo o eliminarlo si fue un error. */
@Component({
  selector: 'app-cargos-dialogo',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatAutocompleteModule,
    MatDatepickerModule,
  ],
  providers: [proveerFechas()],
  template: `
    <h2 mat-dialog-title>Cargos de {{ voluntario().nombres }} {{ voluntario().apellidos }}</h2>
    <mat-dialog-content>
      @if (voluntario().cargos.length === 0) {
        <p class="texto-secundario m-0">Aún no tiene cargos registrados.</p>
      } @else {
        <mat-list class="lista">
          @for (c of voluntario().cargos; track c.id) {
            <mat-list-item [class.terminado]="!c.vigente">
              <mat-icon matListItemIcon>{{ c.vigente ? 'badge' : 'history' }}</mat-icon>
              <span matListItemTitle>{{ c.cargo }}{{ c.unidad ? ' · ' + c.unidad : '' }}</span>
              <span matListItemLine>
                Desde {{ fecha(c.fechaInicio) }}{{ c.fechaTermino ? ' hasta ' + fecha(c.fechaTermino) : '' }}
                {{ c.vigente ? '· Vigente' : '' }}
              </span>
              <div matListItemMeta class="flex">
                @if (c.vigente) {
                  <button mat-icon-button (click)="terminar(c)" matTooltip="Terminar cargo" aria-label="Terminar cargo">
                    <mat-icon>event_busy</mat-icon>
                  </button>
                }
                <button mat-icon-button (click)="eliminar(c)" matTooltip="Eliminar (registro erróneo)" aria-label="Eliminar cargo">
                  <mat-icon>delete</mat-icon>
                </button>
              </div>
            </mat-list-item>
          }
        </mat-list>
      }

      <h3 class="subtitulo mt-3 mb-2">Asignar cargo</h3>
      <form [formGroup]="formulario" id="form-cargo" class="grilla-form" (ngSubmit)="agregar()">
        <mat-form-field appearance="outline" class="completo">
          <mat-label>Cargo</mat-label>
          <input matInput formControlName="cargo" [matAutocomplete]="auto" maxlength="60" />
          <mat-autocomplete #auto="matAutocomplete">
            @for (s of sugerencias(); track s) {
              <mat-option [value]="s">{{ s }}</mat-option>
            }
          </mat-autocomplete>
          <mat-error>Indica el cargo.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Unidad (opcional)</mat-label>
          <mat-select formControlName="unidadId">
            <mat-option [value]="null">Cargo de grupo</mat-option>
            @for (u of unidades(); track u.id) {
              <mat-option [value]="u.id">{{ u.nombre }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Desde</mat-label>
          <input matInput formControlName="fechaInicio" [matDatepicker]="picker" [max]="hoy" placeholder="dd-mm-aaaa" />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
          <mat-error>Ingresa una fecha válida.</mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close type="button">Cerrar</button>
      <button mat-flat-button type="submit" form="form-cargo">Asignar cargo</button>
    </mat-dialog-actions>
  `,
  styles: `
    .lista {
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px;
    }
    .terminado {
      opacity: 0.7;
    }
    .subtitulo {
      font: var(--mat-sys-title-small);
    }
    .grilla-form {
      display: grid;
      grid-template-columns: 1fr 1fr;
      column-gap: 16px;
    }
    .completo {
      grid-column: 1 / -1;
    }
    @media (max-width: 599px) {
      .grilla-form {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class CargosDialogo {
  private readonly servicio = inject(VoluntariosService);
  private readonly dialogo = inject(MatDialog);
  protected readonly voluntario = signal(inject<VoluntarioDetalle>(MAT_DIALOG_DATA));
  protected readonly unidades = signal<Unidad[]>([]);
  protected readonly hoy = new Date();
  protected readonly fecha = formatearFecha;
  private readonly directivaFormulario = viewChild.required(FormGroupDirective);

  protected readonly formulario = inject(FormBuilder).group({
    cargo: ['', [Validators.required, Validators.maxLength(60)]],
    unidadId: [null as number | null],
    fechaInicio: [new Date() as Date | null, Validators.required],
  });

  private readonly textoCargo = toSignal(this.formulario.controls.cargo.valueChanges.pipe(startWith('')));
  protected readonly sugerencias = computed(() => {
    const texto = (this.textoCargo() ?? '').toLowerCase();
    return CARGOS_SUGERIDOS.filter((c) => c.toLowerCase().includes(texto));
  });

  constructor() {
    inject(UnidadesService).listar().subscribe((u) => this.unidades.set(u));
  }

  protected agregar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const valor = this.formulario.getRawValue();
    this.servicio
      .agregarCargo(this.voluntario().id, {
        cargo: valor.cargo!.trim(),
        unidadId: valor.unidadId,
        fechaInicio: aFechaIso(valor.fechaInicio!),
        fechaTermino: null,
      })
      .subscribe((v) => {
        this.voluntario.set(v);
        // resetForm también limpia el estado "enviado", para que no aparezcan errores en el formulario vacío.
        this.directivaFormulario().resetForm({ cargo: '', unidadId: null, fechaInicio: new Date() });
      });
  }

  protected terminar(cargo: Cargo): void {
    confirmar(this.dialogo, {
      titulo: 'Terminar cargo',
      mensaje: `El cargo "${cargo.cargo}" quedará terminado con fecha de hoy y pasará al historial.`,
      confirmar: 'Terminar',
    })
      .pipe(
        filter(Boolean),
        concatMap(() => this.servicio.terminarCargo(this.voluntario().id, cargo.id, null)),
      )
      .subscribe((v) => this.voluntario.set(v));
  }

  protected eliminar(cargo: Cargo): void {
    confirmar(this.dialogo, {
      titulo: 'Eliminar cargo',
      mensaje: `Se eliminará el registro "${cargo.cargo}". Úsalo solo si fue un error; para cerrar un cargo, termínalo.`,
      confirmar: 'Eliminar',
      peligroso: true,
    })
      .pipe(
        filter(Boolean),
        concatMap(() => this.servicio.eliminarCargo(this.voluntario().id, cargo.id)),
      )
      .subscribe((v) => this.voluntario.set(v));
  }
}

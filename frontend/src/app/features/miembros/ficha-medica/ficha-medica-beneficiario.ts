import { DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import {
  FichaMedica,
  GRUPOS_SANGUINEOS,
  PREVISIONES,
  SolicitudFichaMedica,
} from '../miembros.models';
import { FichaMedicaService } from '../miembros.service';

/** Campos de texto largo de la ficha, en el orden en que se muestran. */
const CAMPOS: {
  clave: keyof SolicitudFichaMedica;
  etiqueta: string;
  icono: string;
  ayuda: string;
}[] = [
  {
    clave: 'alergias',
    etiqueta: 'Alergias',
    icono: 'coronavirus',
    ayuda: 'Medicamentos, alimentos, picaduras…',
  },
  {
    clave: 'enfermedades',
    etiqueta: 'Enfermedades o condiciones',
    icono: 'medical_information',
    ayuda: 'Asma, diabetes, epilepsia…',
  },
  {
    clave: 'medicamentos',
    etiqueta: 'Medicamentos',
    icono: 'medication',
    ayuda: 'Nombre, dosis y horario.',
  },
  {
    clave: 'restriccionesAlimentarias',
    etiqueta: 'Restricciones alimentarias',
    icono: 'no_food',
    ayuda: 'Celíaco, vegetariano…',
  },
  {
    clave: 'necesidadesEmocionales',
    etiqueta: 'Necesidades emocionales',
    icono: 'psychology',
    ayuda: 'Lo que el equipo de dirigentes debe saber para acompañarlo.',
  },
];

/** Pestaña "Ficha médica": se ve en modo lectura y se edita en el mismo lugar. Los datos viajan y se guardan cifrados. */
@Component({
  selector: 'app-ficha-medica-beneficiario',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  template: `
    @if (ficha(); as f) {
      <div class="flex items-center justify-between gap-2 mt-3 mb-3 encabezado">
        <p class="flex items-center gap-1 texto-secundario m-0">
          <mat-icon class="icono">lock</mat-icon>
          @if (f.existe) {
            Actualizada el {{ f.actualizadaEl | date: 'dd-MM-yyyy HH:mm'
            }}{{ f.actualizadaPor ? ' por ' + f.actualizadaPor : '' }}.
          } @else {
            Aún no tiene ficha médica registrada.
          }
          Información confidencial, solo para administración y dirigentes.
        </p>
        @if (!editando()) {
          <button mat-stroked-button (click)="editar()">
            <mat-icon>{{ f.existe ? 'edit' : 'add' }}</mat-icon>
            {{ f.existe ? 'Editar ficha' : 'Completar ficha' }}
          </button>
        }
      </div>

      @if (editando()) {
        <form [formGroup]="formulario" class="formulario" (ngSubmit)="guardar()">
          <div class="flex gap-2 fila">
            <mat-form-field appearance="outline" class="flex-1">
              <mat-label>Previsión de salud</mat-label>
              <mat-select formControlName="prevision">
                <mat-option [value]="null">Sin información</mat-option>
                @for (p of previsiones; track p) {
                  <mat-option [value]="p">{{ p }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" class="flex-1">
              <mat-label>Grupo sanguíneo</mat-label>
              <mat-select formControlName="grupoSanguineo">
                <mat-option [value]="null">Sin información</mat-option>
                @for (g of grupos; track g) {
                  <mat-option [value]="g">{{ g }}</mat-option>
                }
              </mat-select>
            </mat-form-field>
          </div>
          @for (campo of campos; track campo.clave) {
            <mat-form-field appearance="outline" class="ancho-completo">
              <mat-label>{{ campo.etiqueta }}</mat-label>
              <textarea
                matInput
                [formControlName]="campo.clave"
                rows="2"
                maxlength="1000"
              ></textarea>
              <mat-hint>{{ campo.ayuda }}</mat-hint>
            </mat-form-field>
          }
          <div class="flex gap-2 justify-end mt-2">
            <button mat-button type="button" (click)="editando.set(false)">Cancelar</button>
            <button mat-flat-button type="submit">Guardar ficha</button>
          </div>
        </form>
      } @else if (f.existe) {
        <dl class="datos">
          <div>
            <dt>Previsión de salud</dt>
            <dd>{{ f.prevision ?? 'Sin información' }}</dd>
          </div>
          <div>
            <dt>Grupo sanguíneo</dt>
            <dd>{{ f.grupoSanguineo ?? 'Sin información' }}</dd>
          </div>
          @for (campo of campos; track campo.clave) {
            <div class="completo">
              <dt class="flex items-center gap-1">
                <mat-icon class="icono">{{ campo.icono }}</mat-icon>{{ campo.etiqueta }}
              </dt>
              <dd>{{ f[campo.clave] ?? 'Sin información' }}</dd>
            </div>
          }
        </dl>
      }
    }
  `,
  styles: `
    .icono {
      width: 18px;
      height: 18px;
      font-size: 18px;
    }
    .formulario {
      max-width: 760px;
    }
    .datos {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 16px 24px;
      margin: 0;
      max-width: 760px;
      dt {
        font: var(--mat-sys-label-medium);
        color: var(--mat-sys-on-surface-variant);
      }
      dd {
        font: var(--mat-sys-body-large);
        margin: 4px 0 0;
        white-space: pre-line;
      }
    }
    .completo {
      grid-column: 1 / -1;
    }
    @media (max-width: 599px) {
      .encabezado,
      .fila {
        flex-direction: column;
        align-items: stretch;
        gap: 0;
      }
    }
  `,
})
export class FichaMedicaBeneficiario {
  readonly beneficiarioId = input.required<number>();

  private readonly servicio = inject(FichaMedicaService);
  protected readonly ficha = signal<FichaMedica | null>(null);
  protected readonly editando = signal(false);
  protected readonly campos = CAMPOS;
  protected readonly grupos = GRUPOS_SANGUINEOS;
  protected readonly previsiones = PREVISIONES;

  protected readonly formulario = inject(FormBuilder).group({
    prevision: [null as string | null],
    grupoSanguineo: [null as string | null],
    alergias: [null as string | null],
    enfermedades: [null as string | null],
    medicamentos: [null as string | null],
    restriccionesAlimentarias: [null as string | null],
    necesidadesEmocionales: [null as string | null],
  });

  constructor() {
    effect(() => this.servicio.obtener(this.beneficiarioId()).subscribe((f) => this.ficha.set(f)));
  }

  protected editar(): void {
    const f = this.ficha();
    if (f) this.formulario.reset(f);
    this.editando.set(true);
  }

  protected guardar(): void {
    const valor = this.formulario.getRawValue();
    const limpio = Object.fromEntries(
      Object.entries(valor).map(([clave, texto]) => [clave, texto?.trim() || null]),
    ) as SolicitudFichaMedica;
    this.servicio.guardar(this.beneficiarioId(), limpio).subscribe((f) => {
      this.ficha.set(f);
      this.editando.set(false);
    });
  }
}

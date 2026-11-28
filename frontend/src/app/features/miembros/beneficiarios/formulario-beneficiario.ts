import { Component, computed, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Observable, of, startWith, switchMap } from 'rxjs';
import { aFechaIso, desdeFechaIso, proveerFechas } from '../../../core/i18n/fechas';
import { RutRegistrado, rutDisponible, validarRut } from '../../../shared/rut/rut';
import {
  BeneficiarioDetalle,
  capitalizar,
  etiquetaRama,
  SolicitudBeneficiario,
  SUBGRUPO_GENERICO,
  Unidad,
} from '../miembros.models';

/** Formulario del beneficiario en tres grupos (uno por paso del stepper): identificación, datos personales y unidad. */
export function crearFormularioBeneficiario(
  fb: FormBuilder,
  existe: (rut: string) => Observable<RutRegistrado>,
  b?: BeneficiarioDetalle,
) {
  return fb.group({
    identificacion: fb.group({
      rut: [
        b?.rut ?? '',
        {
          validators: [Validators.required, validarRut],
          asyncValidators: [rutDisponible(existe, () => b?.id)],
        },
      ],
    }),
    personales: fb.group({
      nombres: [b?.nombres ?? '', [Validators.required, Validators.maxLength(80)]],
      apellidos: [b?.apellidos ?? '', [Validators.required, Validators.maxLength(80)]],
      fechaNacimiento: [
        b ? desdeFechaIso(b.fechaNacimiento) : (null as Date | null),
        Validators.required,
      ],
      direccion: [b?.direccion ?? '', Validators.maxLength(200)],
    }),
    ubicacion: fb.group({
      unidadId: [
        { value: b?.unidadId ?? (null as number | null), disabled: b?.estado === 'retirado' },
        Validators.required,
      ],
      patrullaId: [
        { value: b?.patrullaId ?? (null as number | null), disabled: b?.estado === 'retirado' },
      ],
      fechaIngreso: [b ? desdeFechaIso(b.fechaIngreso) : new Date(), Validators.required],
      registroInstitucionalAnio: [
        b?.registroInstitucionalAnio ?? (null as number | null),
        [Validators.min(1900), Validators.max(2100)],
      ],
    }),
  });
}

export type FormularioBeneficiario = ReturnType<typeof crearFormularioBeneficiario>;

export function aSolicitudBeneficiario(formulario: FormularioBeneficiario): SolicitudBeneficiario {
  const { identificacion, personales, ubicacion } = formulario.getRawValue();
  return {
    rut: identificacion.rut!,
    nombres: personales.nombres!.trim(),
    apellidos: personales.apellidos!.trim(),
    fechaNacimiento: aFechaIso(personales.fechaNacimiento!),
    direccion: personales.direccion?.trim() || null,
    unidadId: ubicacion.unidadId!,
    patrullaId: ubicacion.patrullaId ?? null,
    fechaIngreso: aFechaIso(ubicacion.fechaIngreso!),
    registroInstitucionalAnio: ubicacion.registroInstitucionalAnio ?? null,
  };
}

/** Campos de datos personales (nombres, apellidos, nacimiento y dirección). */
@Component({
  selector: 'app-campos-personales',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatDatepickerModule],
  providers: [proveerFechas()],
  template: `
    <div [formGroup]="grupo()" class="grilla-campos">
      <mat-form-field appearance="outline">
        <mat-label>Nombres</mat-label>
        <input matInput formControlName="nombres" maxlength="80" />
        <mat-hint align="end">{{ largo('nombres') }}/80</mat-hint>
        <mat-error>Ingresa los nombres.</mat-error>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Apellidos</mat-label>
        <input matInput formControlName="apellidos" maxlength="80" />
        <mat-hint align="end">{{ largo('apellidos') }}/80</mat-hint>
        <mat-error>Ingresa los apellidos.</mat-error>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Fecha de nacimiento</mat-label>
        <input
          matInput
          formControlName="fechaNacimiento"
          [matDatepicker]="picker"
          [max]="hoy"
          placeholder="dd-mm-aaaa"
        />
        <mat-datepicker-toggle matIconSuffix [for]="picker" />
        <mat-datepicker #picker startView="multi-year" />
        <mat-error>Ingresa una fecha válida (dd-mm-aaaa).</mat-error>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Dirección (opcional)</mat-label>
        <input matInput formControlName="direccion" maxlength="200" />
        <mat-hint align="end">{{ largo('direccion') }}/200</mat-hint>
      </mat-form-field>
    </div>
  `,
  styles: `
    .grilla-campos {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      column-gap: 16px;
      row-gap: 4px;
    }
    @media (max-width: 599px) {
      .grilla-campos {
        grid-template-columns: minmax(0, 1fr);
      }
    }
  `,
})
export class CamposPersonales {
  readonly grupo = input.required<FormGroup>();
  protected readonly hoy = new Date();
  protected largo = (campo: string) => (this.grupo().get(campo)?.value ?? '').length;
}

/** Campos de unidad, patrulla, fecha de ingreso y registro institucional. */
@Component({
  selector: 'app-campos-ubicacion',
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
  ],
  providers: [proveerFechas()],
  template: `
    <div [formGroup]="grupo()" class="grilla-campos">
      <mat-form-field appearance="outline">
        <mat-label>Unidad</mat-label>
        <mat-select
          formControlName="unidadId"
          (selectionChange)="grupo().get('patrullaId')?.setValue(null)"
        >
          @for (u of unidades(); track u.id) {
            <mat-option
              [value]="u.id"
              [disabled]="u.beneficiarios >= u.maximo && u.id !== unidadActual()"
            >
              {{ u.nombre }} · {{ etiquetaRama(u.rama) }}
              {{ u.beneficiarios >= u.maximo ? '(completa)' : '' }}
            </mat-option>
          }
        </mat-select>
        @if (grupo().get('unidadId')?.disabled) {
          <mat-hint>Está retirado: para cambiarlo de unidad, reingrésalo.</mat-hint>
        }
        <mat-error>Selecciona la unidad.</mat-error>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>{{ capitalizar(subgrupo().nombre) }} (opcional)</mat-label>
        <mat-select formControlName="patrullaId">
          <mat-option [value]="null">Sin {{ subgrupo().nombre }}</mat-option>
          @for (p of patrullas(); track p.id) {
            <mat-option
              [value]="p.id"
              [disabled]="completa(p.beneficiarios) && p.id !== patrullaActual()"
            >
              {{ p.nombre }}
              @if (subgrupo().maximo) {
                · {{ p.beneficiarios }}/{{ subgrupo().maximo }}
              }
            </mat-option>
          }
        </mat-select>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Fecha de ingreso</mat-label>
        <input
          matInput
          formControlName="fechaIngreso"
          [matDatepicker]="picker"
          [max]="hoy"
          placeholder="dd-mm-aaaa"
        />
        <mat-datepicker-toggle matIconSuffix [for]="picker" />
        <mat-datepicker #picker />
        <mat-error>Ingresa una fecha válida (dd-mm-aaaa).</mat-error>
      </mat-form-field>
      <mat-form-field appearance="outline">
        <mat-label>Año de registro institucional (opcional)</mat-label>
        <input
          matInput
          type="number"
          formControlName="registroInstitucionalAnio"
          min="1900"
          max="2100"
        />
        <mat-error>Ingresa un año válido.</mat-error>
      </mat-form-field>
    </div>
  `,
  styles: `
    .grilla-campos {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      column-gap: 16px;
      row-gap: 4px;
    }
    @media (max-width: 599px) {
      .grilla-campos {
        grid-template-columns: minmax(0, 1fr);
      }
    }
  `,
})
export class CamposUbicacion {
  readonly grupo = input.required<FormGroup>();
  readonly unidades = input.required<Unidad[]>();
  /** Unidad y patrulla actuales al editar: se pueden mantener aunque estén completas o desactivadas. */
  readonly unidadActual = input<number | null>(null);
  readonly patrullaActual = input<number | null>(null);
  protected readonly hoy = new Date();
  protected readonly etiquetaRama = etiquetaRama;
  protected readonly capitalizar = capitalizar;

  private readonly unidadElegida = toSignal(
    toObservable(this.grupo).pipe(
      switchMap(
        (g) =>
          g.get('unidadId')?.valueChanges.pipe(startWith(g.get('unidadId')?.value)) ?? of(null),
      ),
    ),
  );

  private readonly unidad = computed(() =>
    this.unidades().find((u) => u.id === this.unidadElegida()),
  );
  protected readonly subgrupo = computed(() => this.unidad()?.subgrupo ?? SUBGRUPO_GENERICO);
  protected readonly patrullas = computed(() =>
    (this.unidad()?.patrullas ?? []).filter((p) => p.activa || p.id === this.patrullaActual()),
  );

  protected completa(integrantes: number): boolean {
    const maximo = this.subgrupo().maximo;
    return maximo !== null && integrantes >= maximo;
  }
}

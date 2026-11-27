import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { aFechaIso, desdeFechaIso, proveerFechas } from '../../../core/i18n/fechas';
import { validarRut } from '../../../shared/rut/rut';
import { RutInput } from '../../../shared/rut/rut-input';
import { BeneficiarioDetalle, etiquetaRama, SolicitudBeneficiario, Unidad } from '../miembros.models';

export interface DatosBeneficiarioDialogo {
  beneficiario?: BeneficiarioDetalle;
  unidades: Unidad[];
}

/** Formulario para registrar o editar un beneficiario. Cambiar unidad o patrulla queda en el historial. */
@Component({
  selector: 'app-beneficiario-dialogo',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    RutInput,
  ],
  providers: [proveerFechas()],
  template: `
    <h2 mat-dialog-title>{{ beneficiario ? 'Editar beneficiario' : 'Nuevo beneficiario' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="formulario" id="form-beneficiario" class="grilla-form" (ngSubmit)="guardar()">
        <mat-form-field appearance="outline">
          <mat-label>RUT</mat-label>
          <input matInput formControlName="rut" appRut placeholder="12.345.678-5" maxlength="12" />
          @if (formulario.controls.rut.hasError('required')) {
            <mat-error>Ingresa el RUT.</mat-error>
          } @else if (formulario.controls.rut.hasError('rut')) {
            <mat-error>El RUT no es válido.</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Fecha de nacimiento</mat-label>
          <input matInput formControlName="fechaNacimiento" [matDatepicker]="pickerNacimiento" [max]="hoy" placeholder="dd-mm-aaaa" />
          <mat-datepicker-toggle matIconSuffix [for]="pickerNacimiento" />
          <mat-datepicker #pickerNacimiento startView="multi-year" />
          <mat-error>Ingresa una fecha válida (dd-mm-aaaa).</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Nombres</mat-label>
          <input matInput formControlName="nombres" maxlength="80" />
          <mat-error>Ingresa los nombres.</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Apellidos</mat-label>
          <input matInput formControlName="apellidos" maxlength="80" />
          <mat-error>Ingresa los apellidos.</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" class="completo">
          <mat-label>Dirección (opcional)</mat-label>
          <input matInput formControlName="direccion" maxlength="200" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Unidad</mat-label>
          <mat-select formControlName="unidadId">
            @for (unidad of datos.unidades; track unidad.id) {
              <mat-option [value]="unidad.id">{{ unidad.nombre }} · {{ etiquetaRama(unidad.rama) }}</mat-option>
            }
          </mat-select>
          @if (retirado) {
            <mat-hint>Para cambiarlo de unidad, reingrésalo.</mat-hint>
          }
          <mat-error>Selecciona la unidad.</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Patrulla (opcional)</mat-label>
          <mat-select formControlName="patrullaId">
            <mat-option [value]="null">Sin patrulla</mat-option>
            @for (patrulla of patrullas(); track patrulla.id) {
              <mat-option [value]="patrulla.id">{{ patrulla.nombre }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Fecha de ingreso</mat-label>
          <input matInput formControlName="fechaIngreso" [matDatepicker]="pickerIngreso" [max]="hoy" placeholder="dd-mm-aaaa" />
          <mat-datepicker-toggle matIconSuffix [for]="pickerIngreso" />
          <mat-datepicker #pickerIngreso />
          <mat-error>Ingresa una fecha válida (dd-mm-aaaa).</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Año de registro institucional</mat-label>
          <input matInput type="number" formControlName="registroInstitucionalAnio" min="1900" max="2100" />
          <mat-hint>Opcional</mat-hint>
          <mat-error>Ingresa un año válido.</mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close type="button">Cancelar</button>
      <button mat-flat-button type="submit" form="form-beneficiario">{{ beneficiario ? 'Guardar' : 'Registrar' }}</button>
    </mat-dialog-actions>
  `,
  styles: `
    .grilla-form {
      display: grid;
      grid-template-columns: 1fr 1fr;
      column-gap: 16px;
      row-gap: 4px;
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
export class BeneficiarioDialogo {
  protected readonly datos = inject<DatosBeneficiarioDialogo>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<BeneficiarioDialogo, SolicitudBeneficiario>);
  protected readonly beneficiario = this.datos.beneficiario;
  protected readonly retirado = this.beneficiario?.estado === 'retirado';
  protected readonly hoy = new Date();
  protected readonly etiquetaRama = etiquetaRama;

  protected readonly formulario = inject(FormBuilder).group({
    rut: [this.beneficiario?.rut ?? '', [Validators.required, validarRut]],
    nombres: [this.beneficiario?.nombres ?? '', [Validators.required, Validators.maxLength(80)]],
    apellidos: [this.beneficiario?.apellidos ?? '', [Validators.required, Validators.maxLength(80)]],
    fechaNacimiento: [this.beneficiario ? desdeFechaIso(this.beneficiario.fechaNacimiento) : null, Validators.required],
    direccion: [this.beneficiario?.direccion ?? ''],
    unidadId: [
      { value: this.beneficiario?.unidadId ?? null, disabled: this.retirado },
      Validators.required,
    ],
    patrullaId: [{ value: this.beneficiario?.patrullaId ?? null, disabled: this.retirado }],
    fechaIngreso: [this.beneficiario ? desdeFechaIso(this.beneficiario.fechaIngreso) : new Date(), Validators.required],
    registroInstitucionalAnio: [this.beneficiario?.registroInstitucionalAnio ?? null, [Validators.min(1900), Validators.max(2100)]],
  });

  private readonly unidadSeleccionada = toSignal(this.formulario.controls.unidadId.valueChanges, {
    initialValue: this.formulario.controls.unidadId.value,
  });

  /** Patrullas activas de la unidad elegida (más la actual, aunque esté desactivada). */
  protected readonly patrullas = computed(() => {
    const unidad = this.datos.unidades.find((u) => u.id === this.unidadSeleccionada());
    return (unidad?.patrullas ?? []).filter((p) => p.activa || p.id === this.beneficiario?.patrullaId);
  });

  constructor() {
    this.formulario.controls.unidadId.valueChanges.subscribe(() => this.formulario.controls.patrullaId.setValue(null));
  }

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const valor = this.formulario.getRawValue();
    this.ref.close({
      rut: valor.rut!,
      nombres: valor.nombres!.trim(),
      apellidos: valor.apellidos!.trim(),
      fechaNacimiento: aFechaIso(valor.fechaNacimiento!),
      direccion: valor.direccion?.trim() || null,
      unidadId: valor.unidadId!,
      patrullaId: valor.patrullaId ?? null,
      fechaIngreso: aFechaIso(valor.fechaIngreso!),
      registroInstitucionalAnio: valor.registroInstitucionalAnio ?? null,
    });
  }
}

import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { aFechaIso, proveerFechas } from '../../../core/i18n/fechas';
import { BeneficiarioResumen, SolicitudMovimiento, Unidad } from '../miembros.models';
import { protegerDialogo } from '../../../shared/confirmacion/confirmacion';

export interface DatosMovimientoDialogo {
  tipo: 'retiro' | 'reingreso';
  beneficiario: BeneficiarioResumen;
  /** Solo para reingreso: unidades activas donde puede volver. */
  unidades?: Unidad[];
}

/** Pide la fecha y un comentario para retirar o reingresar a un beneficiario. */
@Component({
  selector: 'app-movimiento-dialogo',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
  ],
  providers: [proveerFechas()],
  template: `
    <h2 mat-dialog-title>{{ esRetiro ? 'Retirar beneficiario' : 'Reingresar beneficiario' }}</h2>
    <mat-dialog-content>
      <p class="m-0 mb-3">
        {{ datos.beneficiario.nombres }} {{ datos.beneficiario.apellidos }}
        {{
          esRetiro
            ? 'quedará como retirado. Sus datos e historial se conservan.'
            : 'volverá a estar activo en el grupo.'
        }}
      </p>
      <form
        [formGroup]="formulario"
        id="form-movimiento"
        class="flex-col gap-1"
        (ngSubmit)="guardar()"
      >
        <mat-form-field appearance="outline">
          <mat-label>{{ esRetiro ? 'Fecha de retiro' : 'Fecha de reingreso' }}</mat-label>
          <input
            matInput
            formControlName="fecha"
            [matDatepicker]="picker"
            [max]="hoy"
            placeholder="dd-mm-aaaa"
          />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
          <mat-error>Ingresa una fecha válida (dd-mm-aaaa).</mat-error>
        </mat-form-field>

        @if (!esRetiro) {
          <mat-form-field appearance="outline">
            <mat-label>Unidad</mat-label>
            <mat-select formControlName="unidadId">
              @for (unidad of datos.unidades ?? []; track unidad.id) {
                <mat-option [value]="unidad.id">{{ unidad.nombre }}</mat-option>
              }
            </mat-select>
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
        }

        <mat-form-field appearance="outline">
          <mat-label>{{ esRetiro ? 'Motivo (opcional)' : 'Comentario (opcional)' }}</mat-label>
          <textarea matInput formControlName="detalle" maxlength="255" rows="2"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancelar()">Cancelar</button>
      <button mat-flat-button type="submit" form="form-movimiento">
        {{ esRetiro ? 'Retirar' : 'Reingresar' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class MovimientoDialogo {
  protected readonly datos = inject<DatosMovimientoDialogo>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<MovimientoDialogo, SolicitudMovimiento>);
  protected readonly esRetiro = this.datos.tipo === 'retiro';
  protected readonly hoy = new Date();

  private readonly unidadActualActiva = this.datos.unidades?.some(
    (u) => u.id === this.datos.beneficiario.unidadId,
  );

  protected readonly formulario = inject(FormBuilder).group({
    fecha: [new Date() as Date | null, Validators.required],
    unidadId: [
      this.unidadActualActiva ? this.datos.beneficiario.unidadId : null,
      this.esRetiro ? [] : [Validators.required],
    ],
    patrullaId: [this.unidadActualActiva ? this.datos.beneficiario.patrullaId : null],
    detalle: [''],
  });
  protected readonly cancelar = protegerDialogo(this.ref, this.formulario);

  private readonly unidadSeleccionada = toSignal(this.formulario.controls.unidadId.valueChanges, {
    initialValue: this.formulario.controls.unidadId.value,
  });

  protected readonly patrullas = computed(() =>
    (this.datos.unidades?.find((u) => u.id === this.unidadSeleccionada())?.patrullas ?? []).filter(
      (p) => p.activa,
    ),
  );

  constructor() {
    this.formulario.controls.unidadId.valueChanges.subscribe(() =>
      this.formulario.controls.patrullaId.setValue(null),
    );
  }

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const valor = this.formulario.getRawValue();
    this.ref.close({
      fecha: aFechaIso(valor.fecha!),
      detalle: valor.detalle?.trim() || null,
      ...(this.esRetiro ? {} : { unidadId: valor.unidadId, patrullaId: valor.patrullaId ?? null }),
    });
  }
}

import { Component, inject } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { aFechaIso, proveerFechas } from '../../../core/i18n/fechas';
import { protegerDialogo } from '../../../shared/confirmacion/confirmacion';
import { SolicitudPrestamo } from '../inventario.models';

export interface DatosPrestamoDialogo {
  nombre: string;
  disponible: number;
}

/** La devolución esperada no puede ser antes de la salida. */
function devolucionDespuesDeSalida(grupo: AbstractControl): ValidationErrors | null {
  const { fechaSalida, fechaDevolucionEsperada } = grupo.value as {
    fechaSalida: Date | null;
    fechaDevolucionEsperada: Date | null;
  };
  return fechaSalida && fechaDevolucionEsperada && fechaDevolucionEsperada < fechaSalida
    ? { devolucionAntes: true }
    : null;
}

/** Registra la salida de un bien: cuánto, para quién, para qué actividad y hasta cuándo. */
@Component({
  selector: 'app-prestamo-dialogo',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
  ],
  providers: [proveerFechas()],
  template: `
    <h2 mat-dialog-title>Prestar {{ datos.nombre }}</h2>
    <mat-dialog-content>
      <form [formGroup]="formulario" id="form-prestamo" class="grilla" (ngSubmit)="guardar()">
        <mat-form-field appearance="outline">
          <mat-label>Cantidad</mat-label>
          <input
            matInput
            type="number"
            formControlName="cantidad"
            min="1"
            [max]="datos.disponible"
          />
          <mat-hint
            >{{ datos.disponible }} disponible{{ datos.disponible === 1 ? '' : 's' }}</mat-hint
          >
          <mat-error>Entre 1 y {{ datos.disponible }}.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Destinatario</mat-label>
          <input
            matInput
            formControlName="destinatario"
            maxlength="120"
            placeholder="Ej.: Tropa Lautaro"
          />
          <mat-hint align="end"
            >{{ formulario.controls.destinatario.value?.length ?? 0 }}/120</mat-hint
          >
          <mat-error>Indica a quién se presta.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline" class="completo">
          <mat-label>Actividad (opcional)</mat-label>
          <input
            matInput
            formControlName="actividad"
            maxlength="120"
            placeholder="Ej.: Campamento de verano"
          />
          <mat-hint align="end"
            >{{ formulario.controls.actividad.value?.length ?? 0 }}/120</mat-hint
          >
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Fecha de salida</mat-label>
          <input
            matInput
            formControlName="fechaSalida"
            [matDatepicker]="salida"
            [max]="hoy"
            placeholder="dd-mm-aaaa"
          />
          <mat-datepicker-toggle matIconSuffix [for]="salida" />
          <mat-datepicker #salida />
          <mat-error>Ingresa una fecha válida.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Devolver a más tardar</mat-label>
          <input
            matInput
            formControlName="fechaDevolucionEsperada"
            [matDatepicker]="devolucion"
            [min]="formulario.controls.fechaSalida.value"
            placeholder="dd-mm-aaaa"
          />
          <mat-datepicker-toggle matIconSuffix [for]="devolucion" />
          <mat-datepicker #devolucion />
          <mat-error>Ingresa la fecha de devolución.</mat-error>
        </mat-form-field>
      </form>
      @if (formulario.hasError('devolucionAntes')) {
        <p class="texto-error m-0">La devolución no puede ser antes de la salida.</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancelar()">Cancelar</button>
      <button mat-flat-button type="submit" form="form-prestamo">Registrar préstamo</button>
    </mat-dialog-actions>
  `,
  styles: `
    .grilla {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      column-gap: 16px;
    }
    .completo {
      grid-column: 1 / -1;
    }
    @media (max-width: 599px) {
      .grilla {
        grid-template-columns: minmax(0, 1fr);
      }
    }
  `,
})
export class PrestamoDialogo {
  protected readonly datos = inject<DatosPrestamoDialogo>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<PrestamoDialogo, SolicitudPrestamo>);
  protected readonly hoy = new Date();

  protected readonly formulario = inject(FormBuilder).group(
    {
      cantidad: [
        1,
        [Validators.required, Validators.min(1), Validators.max(this.datos.disponible)],
      ],
      destinatario: ['', [Validators.required, Validators.maxLength(120)]],
      actividad: ['', Validators.maxLength(120)],
      fechaSalida: [new Date() as Date | null, Validators.required],
      fechaDevolucionEsperada: [null as Date | null, Validators.required],
    },
    { validators: devolucionDespuesDeSalida },
  );
  protected readonly cancelar = protegerDialogo(this.ref, this.formulario);

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const v = this.formulario.getRawValue();
    this.ref.close({
      cantidad: v.cantidad!,
      destinatario: v.destinatario!.trim(),
      actividad: v.actividad?.trim() || null,
      fechaSalida: aFechaIso(v.fechaSalida!),
      fechaDevolucionEsperada: aFechaIso(v.fechaDevolucionEsperada!),
    });
  }
}

import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { aFechaIso, desdeFechaIso, proveerFechas } from '../../../core/i18n/fechas';
import { ESTADOS_DEVOLUCION, opciones, Prestamo } from '../inventario.models';

export interface ResultadoDevolucion {
  fechaDevolucion: string;
  estadoDevolucion: string;
}

/** Registra la vuelta de un préstamo y en qué estado llegó. Si se perdió, se descuenta del inventario. */
@Component({
  selector: 'app-devolucion-dialogo',
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
    <h2 mat-dialog-title>Devolución</h2>
    <mat-dialog-content>
      <p class="m-0 mb-3">
        {{ prestamo.cantidad }} × {{ prestamo.bien }} prestado a
        <strong>{{ prestamo.destinatario }}</strong>.
      </p>
      <form
        [formGroup]="formulario"
        id="form-devolucion"
        class="flex-col gap-1"
        (ngSubmit)="guardar()"
      >
        <mat-form-field appearance="outline">
          <mat-label>Fecha de devolución</mat-label>
          <input
            matInput
            formControlName="fecha"
            [matDatepicker]="picker"
            [min]="salida"
            [max]="hoy"
            placeholder="dd-mm-aaaa"
          />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
          <mat-error>Ingresa una fecha válida.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>¿Cómo volvió?</mat-label>
          <mat-select formControlName="estado">
            @for (e of estados; track e.valor) {
              <mat-option [value]="e.valor">{{ e.etiqueta }}</mat-option>
            }
          </mat-select>
          @if (formulario.controls.estado.value === 'perdido') {
            <mat-hint>Se descontará {{ prestamo.cantidad }} de la cantidad del bien.</mat-hint>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close type="button">Cancelar</button>
      <button mat-flat-button type="submit" form="form-devolucion">Registrar devolución</button>
    </mat-dialog-actions>
  `,
})
export class DevolucionDialogo {
  protected readonly prestamo = inject<Prestamo>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<DevolucionDialogo, ResultadoDevolucion>);
  protected readonly estados = opciones(ESTADOS_DEVOLUCION);
  protected readonly hoy = new Date();
  protected readonly salida = desdeFechaIso(this.prestamo.fechaSalida);

  protected readonly formulario = inject(FormBuilder).group({
    fecha: [new Date() as Date | null, Validators.required],
    estado: ['bueno', Validators.required],
  });

  protected guardar(): void {
    if (this.formulario.invalid) return;
    const { fecha, estado } = this.formulario.getRawValue();
    this.ref.close({ fechaDevolucion: aFechaIso(fecha!), estadoDevolucion: estado! });
  }
}

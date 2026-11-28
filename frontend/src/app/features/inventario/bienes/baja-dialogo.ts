import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { aFechaIso, proveerFechas } from '../../../core/i18n/fechas';
import { protegerDialogo } from '../../../shared/confirmacion/confirmacion';
import { MOTIVOS_BAJA, opciones } from '../inventario.models';

export interface ResultadoBaja {
  motivo: string;
  fecha: string;
  detalle: string | null;
}

/** Motivo, fecha y detalle de la baja. La confirmación final la pide quien abre el diálogo. */
@Component({
  selector: 'app-baja-dialogo',
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
    <h2 mat-dialog-title>Dar de baja {{ nombre }}</h2>
    <mat-dialog-content>
      <p class="texto-secundario m-0 mb-3">
        El bien no se borra: queda en el historial con el motivo de la baja.
      </p>
      <form [formGroup]="formulario" id="form-baja" class="flex-col gap-1" (ngSubmit)="guardar()">
        <mat-form-field appearance="outline">
          <mat-label>Motivo</mat-label>
          <mat-select formControlName="motivo">
            @for (m of motivos; track m.valor) {
              <mat-option [value]="m.valor">{{ m.etiqueta }}</mat-option>
            }
          </mat-select>
          <mat-error>Indica el motivo.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Fecha</mat-label>
          <input
            matInput
            formControlName="fecha"
            [matDatepicker]="picker"
            [max]="hoy"
            placeholder="dd-mm-aaaa"
          />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
          <mat-error>Ingresa una fecha válida.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Detalle (opcional)</mat-label>
          <textarea matInput formControlName="detalle" rows="2" maxlength="255"></textarea>
          <mat-hint align="end">{{ formulario.controls.detalle.value?.length ?? 0 }}/255</mat-hint>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancelar()">Cancelar</button>
      <button mat-flat-button type="submit" form="form-baja" class="boton-peligro">
        Continuar
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .boton-peligro {
      background-color: var(--mat-sys-error);
      color: var(--mat-sys-on-error);
    }
  `,
})
export class BajaDialogo {
  protected readonly nombre = inject<string>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<BajaDialogo, ResultadoBaja>);
  protected readonly motivos = opciones(MOTIVOS_BAJA);
  protected readonly hoy = new Date();

  protected readonly formulario = inject(FormBuilder).group({
    motivo: ['', Validators.required],
    fecha: [new Date() as Date | null, Validators.required],
    detalle: ['', Validators.maxLength(255)],
  });
  protected readonly cancelar = protegerDialogo(this.ref, this.formulario);

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const v = this.formulario.getRawValue();
    this.ref.close({
      motivo: v.motivo!,
      fecha: aFechaIso(v.fecha!),
      detalle: v.detalle?.trim() || null,
    });
  }
}

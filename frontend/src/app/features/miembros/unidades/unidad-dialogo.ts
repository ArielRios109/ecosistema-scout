import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { RAMAS, Unidad } from '../miembros.models';

/** Formulario para crear o editar una unidad (nombre y rama). */
@Component({
  selector: 'app-unidad-dialogo',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>{{ unidad ? 'Editar unidad' : 'Nueva unidad' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="formulario" id="form-unidad" class="flex-col gap-1 mt-2" (ngSubmit)="guardar()">
        <mat-form-field appearance="outline">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="nombre" maxlength="80" placeholder="Ej.: Tropa Halcones" />
          @if (formulario.controls.nombre.hasError('required')) {
            <mat-error>Ingresa el nombre de la unidad.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Rama</mat-label>
          <mat-select formControlName="rama">
            @for (rama of ramas; track rama.valor) {
              <mat-option [value]="rama.valor">{{ rama.etiqueta }}</mat-option>
            }
          </mat-select>
          @if (formulario.controls.rama.hasError('required')) {
            <mat-error>Selecciona la rama.</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close type="button">Cancelar</button>
      <button mat-flat-button type="submit" form="form-unidad">{{ unidad ? 'Guardar' : 'Crear unidad' }}</button>
    </mat-dialog-actions>
  `,
})
export class UnidadDialogo {
  protected readonly unidad = inject<Unidad | undefined>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<UnidadDialogo, { nombre: string; rama: string }>);
  protected readonly ramas = RAMAS;

  protected readonly formulario = inject(FormBuilder).nonNullable.group({
    nombre: [this.unidad?.nombre ?? '', [Validators.required, Validators.maxLength(80)]],
    rama: [this.unidad?.rama ?? '', Validators.required],
  });

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    this.ref.close(this.formulario.getRawValue());
  }
}

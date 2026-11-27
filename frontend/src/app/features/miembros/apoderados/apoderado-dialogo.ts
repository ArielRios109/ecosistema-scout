import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { validarRut } from '../../../shared/rut/rut';
import { RutInput } from '../../../shared/rut/rut-input';
import { ApoderadoResumen, SolicitudApoderado } from '../miembros.models';

/** Formulario para registrar o editar los datos de un apoderado. */
@Component({
  selector: 'app-apoderado-dialogo',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, RutInput],
  template: `
    <h2 mat-dialog-title>{{ apoderado ? 'Editar apoderado' : 'Nuevo apoderado' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="formulario" id="form-apoderado" class="flex-col gap-1" (ngSubmit)="guardar()">
        <mat-form-field appearance="outline">
          <mat-label>RUT</mat-label>
          <input matInput formControlName="rut" appRut placeholder="12.345.678-5" maxlength="12" />
          @if (formulario.controls.rut.hasError('required')) {
            <mat-error>Ingresa el RUT.</mat-error>
          } @else if (formulario.controls.rut.hasError('rut')) {
            <mat-error>El RUT no es válido.</mat-error>
          }
        </mat-form-field>
        <div class="flex gap-2 fila-campos">
          <mat-form-field appearance="outline" class="flex-1">
            <mat-label>Nombres</mat-label>
            <input matInput formControlName="nombres" maxlength="80" />
            <mat-error>Ingresa los nombres.</mat-error>
          </mat-form-field>
          <mat-form-field appearance="outline" class="flex-1">
            <mat-label>Apellidos</mat-label>
            <input matInput formControlName="apellidos" maxlength="80" />
            <mat-error>Ingresa los apellidos.</mat-error>
          </mat-form-field>
        </div>
        <mat-form-field appearance="outline">
          <mat-label>Correo electrónico</mat-label>
          <input matInput type="email" formControlName="email" maxlength="120" />
          <mat-hint>Se usa para su cuenta del Portal de pagos.</mat-hint>
          @if (formulario.controls.email.hasError('required')) {
            <mat-error>Ingresa el correo.</mat-error>
          } @else {
            <mat-error>El correo no es válido.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Teléfono (opcional)</mat-label>
          <input matInput type="tel" formControlName="telefono" maxlength="20" placeholder="+56 9 1234 5678" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close type="button">Cancelar</button>
      <button mat-flat-button type="submit" form="form-apoderado">{{ apoderado ? 'Guardar' : 'Registrar' }}</button>
    </mat-dialog-actions>
  `,
  styles: `
    @media (max-width: 599px) {
      .fila-campos {
        flex-direction: column;
        gap: 0;
      }
    }
  `,
})
export class ApoderadoDialogo {
  protected readonly apoderado = inject<ApoderadoResumen | undefined>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ApoderadoDialogo, SolicitudApoderado>);

  protected readonly formulario = inject(FormBuilder).nonNullable.group({
    rut: [this.apoderado?.rut ?? '', [Validators.required, validarRut]],
    nombres: [this.apoderado?.nombres ?? '', [Validators.required, Validators.maxLength(80)]],
    apellidos: [this.apoderado?.apellidos ?? '', [Validators.required, Validators.maxLength(80)]],
    email: [this.apoderado?.email ?? '', [Validators.required, Validators.email]],
    telefono: [this.apoderado?.telefono ?? ''],
  });

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const valor = this.formulario.getRawValue();
    this.ref.close({
      rut: valor.rut,
      nombres: valor.nombres.trim(),
      apellidos: valor.apellidos.trim(),
      email: valor.email.trim(),
      telefono: valor.telefono.trim() || null,
    });
  }
}

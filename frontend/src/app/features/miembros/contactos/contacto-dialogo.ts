import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ContactoEmergencia, SolicitudContacto } from '../miembros.models';

/** Formulario de un contacto de emergencia (nombre, parentesco y teléfono). */
@Component({
  selector: 'app-contacto-dialogo',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ contacto ? 'Editar contacto' : 'Nuevo contacto de emergencia' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="formulario" id="form-contacto" class="flex-col gap-1" (ngSubmit)="guardar()">
        <mat-form-field appearance="outline">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="nombre" maxlength="120" />
          <mat-error>Ingresa el nombre del contacto.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Parentesco o relación (opcional)</mat-label>
          <input matInput formControlName="parentesco" maxlength="40" placeholder="Ej.: Tía, vecina" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Teléfono</mat-label>
          <input matInput type="tel" formControlName="telefono" maxlength="20" placeholder="+56 9 1234 5678" />
          @if (formulario.controls.telefono.hasError('required')) {
            <mat-error>Ingresa el teléfono.</mat-error>
          } @else {
            <mat-error>El teléfono no es válido.</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close type="button">Cancelar</button>
      <button mat-flat-button type="submit" form="form-contacto">{{ contacto ? 'Guardar' : 'Agregar' }}</button>
    </mat-dialog-actions>
  `,
})
export class ContactoDialogo {
  protected readonly contacto = inject<ContactoEmergencia | undefined>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ContactoDialogo, SolicitudContacto>);

  protected readonly formulario = inject(FormBuilder).nonNullable.group({
    nombre: [this.contacto?.nombre ?? '', [Validators.required, Validators.maxLength(120)]],
    parentesco: [this.contacto?.parentesco ?? ''],
    telefono: [this.contacto?.telefono ?? '', [Validators.required, Validators.pattern(/^\+?[0-9 ()-]{8,20}$/)]],
  });

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const valor = this.formulario.getRawValue();
    this.ref.close({ nombre: valor.nombre.trim(), parentesco: valor.parentesco.trim() || null, telefono: valor.telefono.trim() });
  }
}

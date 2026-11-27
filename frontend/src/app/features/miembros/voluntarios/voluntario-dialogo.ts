import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import {
  errorRutAlEscribir,
  habilitarSegunRut,
  rutDisponible,
  validarRut,
} from '../../../shared/rut/rut';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { VoluntariosService } from '../miembros.service';
import { RutInput } from '../../../shared/rut/rut-input';
import { SolicitudVoluntario, VoluntarioResumen } from '../miembros.models';
import { protegerDialogo } from '../../../shared/confirmacion/confirmacion';

/** Formulario para registrar o editar los datos de un voluntario adulto. */
@Component({
  selector: 'app-voluntario-dialogo',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    RutInput,
  ],
  template: `
    <h2 mat-dialog-title>{{ voluntario ? 'Editar voluntario' : 'Nuevo voluntario' }}</h2>
    <mat-dialog-content>
      <form
        [formGroup]="formulario"
        id="form-voluntario"
        class="flex-col gap-1"
        (ngSubmit)="guardar()"
      >
        <mat-form-field appearance="outline">
          <mat-label>RUT</mat-label>
          <input
            matInput
            formControlName="rut"
            appRut
            placeholder="12.345.678-5"
            [errorStateMatcher]="errorRut"
          />
          @if (formulario.controls.rut.pending) {
            <mat-spinner matSuffix diameter="20" />
          }
          @if (
            !voluntario && formulario.controls.rut.invalid && formulario.controls.rut.untouched
          ) {
            <mat-hint>Ingresa el RUT para continuar.</mat-hint>
          }
          @if (formulario.controls.rut.hasError('required')) {
            <mat-error>Ingresa el RUT.</mat-error>
          } @else if (formulario.controls.rut.hasError('rut')) {
            <mat-error>El RUT no es válido.</mat-error>
          } @else if (formulario.controls.rut.getError('rutRegistrado'); as r) {
            <mat-error>Ya está registrado: {{ r.nombre }}.</mat-error>
          }
        </mat-form-field>
        <div class="flex gap-2 fila-campos">
          <mat-form-field appearance="outline" class="flex-1">
            <mat-label>Nombres</mat-label>
            <input matInput formControlName="nombres" maxlength="80" />
            <mat-hint align="end">{{ formulario.controls.nombres.value.length }}/80</mat-hint>
            <mat-error>Ingresa los nombres.</mat-error>
          </mat-form-field>
          <mat-form-field appearance="outline" class="flex-1">
            <mat-label>Apellidos</mat-label>
            <input matInput formControlName="apellidos" maxlength="80" />
            <mat-hint align="end">{{ formulario.controls.apellidos.value.length }}/80</mat-hint>
            <mat-error>Ingresa los apellidos.</mat-error>
          </mat-form-field>
        </div>
        <mat-form-field appearance="outline">
          <mat-label>Correo electrónico (opcional)</mat-label>
          <input matInput type="email" formControlName="email" maxlength="120" />
          <mat-hint align="end">{{ formulario.controls.email.value.length }}/120</mat-hint>
          <mat-error>El correo no es válido.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Teléfono (opcional)</mat-label>
          <input
            matInput
            type="tel"
            formControlName="telefono"
            maxlength="20"
            placeholder="+56 9 1234 5678"
          />
          <mat-hint align="end">{{ formulario.controls.telefono.value.length }}/20</mat-hint>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancelar()">Cancelar</button>
      <button mat-flat-button type="submit" form="form-voluntario" [disabled]="!formulario.valid">
        {{ voluntario ? 'Guardar' : 'Registrar' }}
      </button>
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
export class VoluntarioDialogo {
  protected readonly voluntario = inject<VoluntarioResumen | undefined>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<VoluntarioDialogo, SolicitudVoluntario>);

  protected readonly formulario = inject(FormBuilder).nonNullable.group({
    rut: [
      this.voluntario?.rut ?? '',
      {
        validators: [Validators.required, validarRut],
        asyncValidators: [
          rutDisponible(inject(VoluntariosService).existe, () => this.voluntario?.id),
        ],
      },
    ],
    nombres: [this.voluntario?.nombres ?? '', [Validators.required, Validators.maxLength(80)]],
    apellidos: [this.voluntario?.apellidos ?? '', [Validators.required, Validators.maxLength(80)]],
    email: [this.voluntario?.email ?? '', [Validators.email, Validators.maxLength(120)]],
    telefono: [this.voluntario?.telefono ?? '', Validators.maxLength(20)],
  });
  protected readonly cancelar = protegerDialogo(this.ref, this.formulario);
  protected readonly errorRut = errorRutAlEscribir;

  constructor() {
    habilitarSegunRut(this.formulario);
  }

  protected guardar(): void {
    if (!this.formulario.valid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const valor = this.formulario.getRawValue();
    this.ref.close({
      rut: valor.rut,
      nombres: valor.nombres.trim(),
      apellidos: valor.apellidos.trim(),
      email: valor.email.trim() || null,
      telefono: valor.telefono.trim() || null,
    });
  }
}

import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { protegerDialogo } from '../../../shared/confirmacion/confirmacion';
import {
  errorRutAlEscribir,
  habilitarSegunRut,
  rutDisponible,
  validarRut,
} from '../../../shared/rut/rut';
import { RutInput } from '../../../shared/rut/rut-input';
import { ApoderadoResumen, SolicitudApoderado } from '../miembros.models';
import { ApoderadosService } from '../miembros.service';

export interface DatosApoderadoDialogo {
  apoderado?: ApoderadoResumen;
  /** Si el RUT ya existe, ofrece usar ese apoderado en vez de mostrar solo el error. */
  permitirExistente?: boolean;
}

/** Lo que devuelve el diálogo: los datos de un apoderado nuevo, o el apoderado ya registrado que se eligió usar. */
export type ResultadoApoderadoDialogo =
  { tipo: 'nuevo'; datos: SolicitudApoderado } | { tipo: 'existente'; id: number; nombre: string };

/** Formulario de apoderado. Primero se ingresa el RUT; el resto se habilita cuando es válido y no está registrado. */
@Component({
  selector: 'app-apoderado-dialogo',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    RutInput,
  ],
  template: `
    <h2 mat-dialog-title>{{ apoderado ? 'Editar apoderado' : 'Nuevo apoderado' }}</h2>
    <mat-dialog-content>
      <form
        [formGroup]="formulario"
        id="form-apoderado"
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
          @if (!apoderado && formulario.controls.rut.invalid && formulario.controls.rut.untouched) {
            <mat-hint>Ingresa el RUT para continuar.</mat-hint>
          }
          @if (formulario.controls.rut.hasError('required')) {
            <mat-error>Ingresa el RUT.</mat-error>
          } @else if (formulario.controls.rut.hasError('rut')) {
            <mat-error>El RUT no es válido.</mat-error>
          } @else if (registrado(); as r) {
            <mat-error>Ya está registrado: {{ r.nombre }}.</mat-error>
          }
        </mat-form-field>
        @if (datos.permitirExistente && registrado(); as r) {
          <button
            mat-stroked-button
            type="button"
            class="mb-2"
            (click)="usarExistente(r.id, r.nombre)"
          >
            <mat-icon>person_check</mat-icon>
            Usar a {{ r.nombre }}
          </button>
        }
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
          <mat-label>Correo electrónico</mat-label>
          <input matInput type="email" formControlName="email" maxlength="120" />
          <mat-hint>Para su cuenta del Portal de pagos.</mat-hint>
          <mat-hint align="end">{{ formulario.controls.email.value.length }}/120</mat-hint>
          @if (formulario.controls.email.hasError('required')) {
            <mat-error>Ingresa el correo.</mat-error>
          } @else {
            <mat-error>El correo no es válido.</mat-error>
          }
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
      <button mat-flat-button type="submit" form="form-apoderado" [disabled]="!formulario.valid">
        {{ apoderado ? 'Guardar' : 'Registrar' }}
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
export class ApoderadoDialogo {
  protected readonly datos: DatosApoderadoDialogo =
    inject<DatosApoderadoDialogo | null>(MAT_DIALOG_DATA) ?? {};
  protected readonly apoderado = this.datos.apoderado;
  protected readonly errorRut = errorRutAlEscribir;
  private readonly ref = inject(MatDialogRef<ApoderadoDialogo, ResultadoApoderadoDialogo>);
  private readonly servicio = inject(ApoderadosService);

  protected readonly formulario = inject(FormBuilder).nonNullable.group({
    rut: [
      this.apoderado?.rut ?? '',
      {
        validators: [Validators.required, validarRut],
        asyncValidators: [rutDisponible(this.servicio.existe, () => this.apoderado?.id)],
      },
    ],
    nombres: [this.apoderado?.nombres ?? '', [Validators.required, Validators.maxLength(80)]],
    apellidos: [this.apoderado?.apellidos ?? '', [Validators.required, Validators.maxLength(80)]],
    email: [
      this.apoderado?.email ?? '',
      [Validators.required, Validators.email, Validators.maxLength(120)],
    ],
    telefono: [this.apoderado?.telefono ?? '', Validators.maxLength(20)],
  });
  protected readonly cancelar = protegerDialogo(this.ref, this.formulario);

  constructor() {
    habilitarSegunRut(this.formulario);
  }

  protected registrado(): { id: number; nombre: string } | null {
    return this.formulario.controls.rut.getError('rutRegistrado') ?? null;
  }

  protected usarExistente(id: number, nombre: string): void {
    this.ref.close({ tipo: 'existente', id, nombre });
  }

  protected guardar(): void {
    if (!this.formulario.valid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const valor = this.formulario.getRawValue();
    this.ref.close({
      tipo: 'nuevo',
      datos: {
        rut: valor.rut,
        nombres: valor.nombres.trim(),
        apellidos: valor.apellidos.trim(),
        email: valor.email.trim(),
        telefono: valor.telefono.trim() || null,
      },
    });
  }
}

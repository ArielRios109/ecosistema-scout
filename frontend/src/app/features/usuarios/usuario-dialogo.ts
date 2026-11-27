import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { NOMBRE_ROL } from '../../core/auth/permisos';
import { RolInfo, UsuarioResumen } from './usuarios.service';

export interface DatosUsuarioDialogo {
  usuario?: UsuarioResumen;
  roles: RolInfo[];
}

export interface ResultadoUsuarioDialogo {
  nombre: string;
  email: string;
  roles: string[];
}

/** Formulario para crear o editar un usuario (nombre, correo y roles). */
@Component({
  selector: 'app-usuario-dialogo',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>{{ datos.usuario ? 'Editar usuario' : 'Nuevo usuario' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="formulario" class="flex-col gap-1 mt-2" id="form-usuario" (ngSubmit)="guardar()">
        <mat-form-field appearance="outline">
          <mat-label>Nombre</mat-label>
          <input matInput formControlName="nombre" maxlength="120" />
          @if (formulario.controls.nombre.hasError('required')) {
            <mat-error>Ingresa el nombre.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Correo electrónico</mat-label>
          <input matInput type="email" formControlName="email" maxlength="120" />
          @if (formulario.controls.email.hasError('required')) {
            <mat-error>Ingresa el correo.</mat-error>
          } @else if (formulario.controls.email.hasError('email')) {
            <mat-error>El correo no es válido.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Roles</mat-label>
          <mat-select formControlName="roles" multiple>
            @for (rol of datos.roles; track rol.nombre) {
              <mat-option [value]="rol.nombre">{{ nombreRol(rol.nombre) }}</mat-option>
            }
          </mat-select>
          <mat-hint>Los roles se suman: una persona puede tener varios.</mat-hint>
          @if (formulario.controls.roles.hasError('required')) {
            <mat-error>Asigna al menos un rol.</mat-error>
          }
        </mat-form-field>
        @if (!datos.usuario) {
          <p class="texto-secundario m-0 mt-2">
            Se generará una contraseña temporal que la persona deberá cambiar al ingresar por primera vez.
          </p>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close type="button">Cancelar</button>
      <button mat-flat-button type="submit" form="form-usuario">
        {{ datos.usuario ? 'Guardar cambios' : 'Crear usuario' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class UsuarioDialogo {
  protected readonly datos = inject<DatosUsuarioDialogo>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<UsuarioDialogo, ResultadoUsuarioDialogo>);

  protected readonly formulario = inject(FormBuilder).nonNullable.group({
    nombre: [this.datos.usuario?.nombre ?? '', [Validators.required, Validators.maxLength(120)]],
    email: [this.datos.usuario?.email ?? '', [Validators.required, Validators.email, Validators.maxLength(120)]],
    roles: [this.datos.usuario?.roles ?? ([] as string[]), Validators.required],
  });

  protected nombreRol(rol: string): string {
    return NOMBRE_ROL[rol] ?? rol;
  }

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    this.ref.close(this.formulario.getRawValue());
  }
}

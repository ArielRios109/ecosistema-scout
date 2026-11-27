import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { Auth } from '../../core/auth/auth';

/** La confirmación debe coincidir con la nueva contraseña. */
function coinciden(grupo: AbstractControl): ValidationErrors | null {
  return grupo.get('passwordNueva')?.value === grupo.get('confirmacion')?.value
    ? null
    : { noCoinciden: true };
}

/** Política de contraseñas: 8 caracteres, con mayúscula, minúscula y número (igual que la API). */
const POLITICA = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;

/** Cambio de contraseña: voluntario desde el menú de usuario u obligatorio tras ingresar con una temporal. */
@Component({
  selector: 'app-cambiar-password',
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
  ],
  template: `
    <mat-card appearance="outlined" class="tarjeta">
      <mat-card-content>
        <h1 class="titulo-chico m-0">Cambiar mi contraseña</h1>
        @if (auth.debeCambiarPassword()) {
          <p class="aviso p-3 mt-3 m-0">
            Ingresaste con una contraseña temporal. Crea tu propia contraseña para empezar a usar
            Talí.
          </p>
        }
        <form [formGroup]="formulario" (ngSubmit)="guardar()" class="flex-col gap-1 mt-3">
          <mat-form-field appearance="outline">
            <mat-label>{{
              auth.debeCambiarPassword() ? 'Contraseña temporal' : 'Contraseña actual'
            }}</mat-label>
            <input
              matInput
              type="password"
              formControlName="passwordActual"
              maxlength="100"
              autocomplete="current-password"
            />
            @if (formulario.controls.passwordActual.hasError('required')) {
              <mat-error>Ingresa tu contraseña actual.</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Nueva contraseña</mat-label>
            <input
              matInput
              [type]="ver() ? 'text' : 'password'"
              formControlName="passwordNueva"
              maxlength="100"
              autocomplete="new-password"
            />
            <button
              mat-icon-button
              matSuffix
              type="button"
              (click)="ver.set(!ver())"
              [attr.aria-label]="ver() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
            >
              <mat-icon>{{ ver() ? 'visibility_off' : 'visibility' }}</mat-icon>
            </button>
            <mat-hint>Mínimo 8 caracteres, con mayúscula, minúscula y número.</mat-hint>
            @if (formulario.controls.passwordNueva.hasError('required')) {
              <mat-error>Ingresa la nueva contraseña.</mat-error>
            } @else if (formulario.controls.passwordNueva.hasError('pattern')) {
              <mat-error>Mínimo 8 caracteres, con mayúscula, minúscula y número.</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Repite la nueva contraseña</mat-label>
            <input
              matInput
              [type]="ver() ? 'text' : 'password'"
              formControlName="confirmacion"
              maxlength="100"
              autocomplete="new-password"
            />
            @if (formulario.hasError('noCoinciden') && formulario.controls.confirmacion.touched) {
              <mat-hint class="texto-error">Las contraseñas no coinciden.</mat-hint>
            }
          </mat-form-field>
          <button mat-flat-button type="submit" class="mt-2" [disabled]="guardando()">
            Guardar contraseña
          </button>
        </form>
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    .tarjeta {
      max-width: 440px;
    }
    .aviso {
      border-radius: 12px;
      background: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
    }
  `,
})
export class CambiarPassword {
  protected readonly auth = inject(Auth);
  private readonly router = inject(Router);

  protected readonly ver = signal(false);
  protected readonly guardando = signal(false);

  protected readonly formulario = inject(FormBuilder).nonNullable.group(
    {
      passwordActual: ['', Validators.required],
      passwordNueva: ['', [Validators.required, Validators.pattern(POLITICA)]],
      confirmacion: ['', Validators.required],
    },
    { validators: coinciden },
  );

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    this.guardando.set(true);
    const { passwordActual, passwordNueva } = this.formulario.getRawValue();
    this.auth.cambiarPassword(passwordActual, passwordNueva).subscribe({
      next: () => this.router.navigate(['/inicio']),
      error: () => this.guardando.set(false),
    });
  }
}

import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { protegerDialogo } from '../confirmacion/confirmacion';

export interface DatosTextoDialogo {
  titulo: string;
  etiqueta: string;
  valor?: string;
  maximo?: number;
  confirmar?: string;
}

/** Diálogo reutilizable para pedir un solo texto (por ejemplo, el nombre de una patrulla). */
@Component({
  selector: 'app-texto-dialogo',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ datos.titulo }}</h2>
    <mat-dialog-content>
      <form [formGroup]="formulario" id="form-texto" (ngSubmit)="guardar()">
        <mat-form-field appearance="outline" class="ancho-completo">
          <mat-label>{{ datos.etiqueta }}</mat-label>
          <input matInput [formControl]="control" [maxlength]="datos.maximo ?? 120" />
          <mat-hint align="end">{{ control.value.length }}/{{ datos.maximo ?? 120 }}</mat-hint>
          @if (control.hasError('required')) {
            <mat-error>Este campo es obligatorio.</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancelar()">Cancelar</button>
      <button mat-flat-button type="submit" form="form-texto">
        {{ datos.confirmar ?? 'Guardar' }}
      </button>
    </mat-dialog-actions>
  `,
})
export class TextoDialogo {
  protected readonly datos = inject<DatosTextoDialogo>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<TextoDialogo, string>);
  protected readonly control = new FormControl(this.datos.valor ?? '', {
    nonNullable: true,
    validators: [Validators.required],
  });
  protected readonly formulario = new FormGroup({ texto: this.control });
  protected readonly cancelar = protegerDialogo(this.ref, this.formulario);

  protected guardar(): void {
    if (this.control.invalid || !this.control.value.trim()) {
      this.control.markAsTouched();
      return;
    }
    this.ref.close(this.control.value.trim());
  }
}

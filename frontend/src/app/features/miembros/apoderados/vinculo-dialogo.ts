import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, filter, map, of, startWith, switchMap } from 'rxjs';
import { ApoderadoResumen, PARENTESCOS, VinculoApoderado } from '../miembros.models';
import { ApoderadosService } from '../miembros.service';
import { protegerDialogo } from '../../../shared/confirmacion/confirmacion';

export interface DatosVinculoDialogo {
  nombreBeneficiario: string;
  /** Si viene, se edita ese vínculo (no se elige apoderado). */
  vinculo?: VinculoApoderado;
}

/** Resultado: el apoderado elegido (o "nuevo" para registrarlo primero), el parentesco y si es principal. */
export interface ResultadoVinculo {
  apoderadoId: number | 'nuevo';
  parentesco: string;
  esPrincipal: boolean;
}

/** Vincula un apoderado (buscándolo por nombre, correo o RUT) o edita el parentesco de un vínculo existente. */
@Component({
  selector: 'app-vinculo-dialogo',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatAutocompleteModule,
    MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ datos.vinculo ? 'Editar vínculo' : 'Vincular apoderado' }}</h2>
    <mat-dialog-content>
      <form
        [formGroup]="formulario"
        id="form-vinculo"
        class="flex-col gap-1"
        (ngSubmit)="guardar()"
      >
        @if (datos.vinculo) {
          <p class="m-0 mb-2">
            <strong>{{ datos.vinculo.nombre }}</strong> · apoderado de
            {{ datos.nombreBeneficiario }}
          </p>
        } @else {
          <mat-form-field appearance="outline">
            <mat-label>Buscar apoderado</mat-label>
            <mat-icon matPrefix>search</mat-icon>
            <input
              matInput
              formControlName="busqueda"
              [matAutocomplete]="auto"
              placeholder="Nombre, correo o RUT"
            />
            <mat-autocomplete
              #auto="matAutocomplete"
              [displayWith]="mostrar"
              (optionSelected)="elegir($event.option.value)"
            >
              @for (a of resultados(); track a.id) {
                <mat-option [value]="a">
                  {{ a.nombres }} {{ a.apellidos }}
                  <small class="texto-secundario">· {{ a.rut }}</small>
                </mat-option>
              }
            </mat-autocomplete>
            @if (
              formulario.controls.apoderadoId.touched && formulario.controls.apoderadoId.invalid
            ) {
              <mat-hint class="texto-error"
                >Elige un apoderado de la lista o registra uno nuevo.</mat-hint
              >
            }
          </mat-form-field>
          <button mat-button type="button" class="nuevo" (click)="registrarNuevo()">
            <mat-icon>person_add</mat-icon>
            Registrar un apoderado nuevo
          </button>
        }

        <mat-form-field appearance="outline">
          <mat-label>Parentesco</mat-label>
          <mat-select formControlName="parentesco">
            @for (p of parentescos; track p) {
              <mat-option [value]="p">{{ p }}</mat-option>
            }
          </mat-select>
          <mat-error>Indica el parentesco.</mat-error>
        </mat-form-field>
        <mat-checkbox formControlName="esPrincipal">Apoderado principal</mat-checkbox>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancelar()">Cancelar</button>
      <button mat-flat-button type="submit" form="form-vinculo">
        {{ datos.vinculo ? 'Guardar' : 'Vincular' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .nuevo {
      align-self: flex-start;
      margin-bottom: 8px;
    }
  `,
})
export class VinculoDialogo {
  protected readonly datos = inject<DatosVinculoDialogo>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<VinculoDialogo, ResultadoVinculo>);
  private readonly servicio = inject(ApoderadosService);
  protected readonly parentescos = PARENTESCOS;

  protected readonly formulario = inject(FormBuilder).group({
    busqueda: [''],
    apoderadoId: [this.datos.vinculo?.apoderadoId ?? (null as number | null), Validators.required],
    parentesco: [this.datos.vinculo?.parentesco ?? '', Validators.required],
    esPrincipal: [this.datos.vinculo?.esPrincipal ?? false],
  });
  protected readonly cancelar = protegerDialogo(this.ref, this.formulario);

  protected readonly resultados = toSignal(
    this.formulario.controls.busqueda.valueChanges.pipe(
      startWith(''),
      filter((valor) => typeof valor === 'string'),
      debounceTime(300),
      switchMap((texto) =>
        texto && texto.trim().length >= 2
          ? this.servicio.listar({ buscar: texto, tamano: 8 }, true).pipe(map((p) => p.elementos))
          : of([] as ApoderadoResumen[]),
      ),
    ),
    { initialValue: [] as ApoderadoResumen[] },
  );

  constructor() {
    // Si se vuelve a escribir después de elegir, se descarta la elección.
    this.formulario.controls.busqueda.valueChanges
      .pipe(filter((valor) => typeof valor === 'string'))
      .subscribe(() => this.formulario.controls.apoderadoId.setValue(null));
  }

  protected mostrar(apoderado: ApoderadoResumen | string | null): string {
    return typeof apoderado === 'object' && apoderado
      ? `${apoderado.nombres} ${apoderado.apellidos}`
      : (apoderado ?? '');
  }

  protected elegir(apoderado: ApoderadoResumen): void {
    this.formulario.controls.apoderadoId.setValue(apoderado.id);
  }

  protected registrarNuevo(): void {
    const { parentesco, esPrincipal } = this.formulario.getRawValue();
    if (!parentesco) {
      this.formulario.controls.parentesco.markAsTouched();
      return;
    }
    this.ref.close({ apoderadoId: 'nuevo', parentesco, esPrincipal: !!esPrincipal });
  }

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const valor = this.formulario.getRawValue();
    this.ref.close({
      apoderadoId: valor.apoderadoId!,
      parentesco: valor.parentesco!,
      esPrincipal: !!valor.esPrincipal,
    });
  }
}

import { Component, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router, RouterLink } from '@angular/router';
import { concatMap, EMPTY, forkJoin, of } from 'rxjs';
import { ConCambiosSinGuardar } from '../../../shared/confirmacion/cambios-sin-guardar.guard';
import { ConfirmacionService } from '../../../shared/confirmacion/confirmacion';
import { errorRutAlEscribir } from '../../../shared/rut/rut';
import { RutInput } from '../../../shared/rut/rut-input';
import { BeneficiarioDetalle, SUBGRUPO_GENERICO, Unidad } from '../miembros.models';
import { BeneficiariosService, UnidadesService } from '../miembros.service';
import {
  aSolicitudBeneficiario,
  CamposPersonales,
  CamposUbicacion,
  crearFormularioBeneficiario,
  FormularioBeneficiario,
} from './formulario-beneficiario';

/** Edición de los datos de un beneficiario. Si cambia de unidad o patrulla, se confirma porque queda en el historial. */
@Component({
  selector: 'app-editar-beneficiario',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    RutInput,
    CamposPersonales,
    CamposUbicacion,
  ],
  template: `
    <a mat-button [routerLink]="['/miembros/beneficiarios', id()]" class="mb-2">
      <mat-icon>arrow_back</mat-icon>
      Volver a la ficha
    </a>
    @if (formulario(); as f) {
      <h2 class="titulo-grande m-0 mb-3">
        Editar a {{ beneficiario()?.nombres }} {{ beneficiario()?.apellidos }}
      </h2>
      <div class="flex-col gap-3 contenido">
        <mat-card appearance="outlined">
          <mat-card-header><mat-card-title>Identificación</mat-card-title></mat-card-header>
          <mat-card-content [formGroup]="f.controls.identificacion" class="mt-3">
            <mat-form-field appearance="outline" class="campo-rut">
              <mat-label>RUT</mat-label>
              <input matInput formControlName="rut" appRut [errorStateMatcher]="errorRut" />
              @if (f.controls.identificacion.controls.rut.pending) {
                <mat-spinner matSuffix diameter="20" />
              }
              @if (f.controls.identificacion.controls.rut.hasError('rut')) {
                <mat-error>El RUT no es válido.</mat-error>
              } @else if (f.controls.identificacion.controls.rut.getError('rutRegistrado'); as r) {
                <mat-error>Ese RUT ya es de {{ r.nombre }}.</mat-error>
              } @else {
                <mat-error>Ingresa el RUT.</mat-error>
              }
            </mat-form-field>
          </mat-card-content>
        </mat-card>
        <mat-card appearance="outlined">
          <mat-card-header><mat-card-title>Datos personales</mat-card-title></mat-card-header>
          <mat-card-content class="mt-3">
            <app-campos-personales [grupo]="f.controls.personales" />
          </mat-card-content>
        </mat-card>
        <mat-card appearance="outlined">
          <mat-card-header><mat-card-title>Unidad</mat-card-title></mat-card-header>
          <mat-card-content class="mt-3">
            <app-campos-ubicacion
              [grupo]="f.controls.ubicacion"
              [unidades]="unidades()"
              [unidadActual]="beneficiario()?.unidadId ?? null"
              [patrullaActual]="beneficiario()?.patrullaId ?? null"
            />
          </mat-card-content>
        </mat-card>
        <div class="flex justify-end gap-2">
          <button mat-button type="button" [routerLink]="['/miembros/beneficiarios', id()]">
            Cancelar
          </button>
          <button mat-flat-button type="button" (click)="guardar()" [disabled]="!f.valid">
            Guardar cambios
          </button>
        </div>
      </div>
    }
  `,
  styles: `
    .contenido {
      max-width: 760px;
    }
    .campo-rut {
      width: 100%;
      max-width: 320px;
    }
  `,
})
export class EditarBeneficiario implements ConCambiosSinGuardar {
  /** Id de la ruta /miembros/beneficiarios/:id/editar. */
  readonly id = input.required<string>();

  private readonly servicio = inject(BeneficiariosService);
  private readonly unidadesServicio = inject(UnidadesService);
  private readonly confirmacion = inject(ConfirmacionService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly beneficiario = signal<BeneficiarioDetalle | null>(null);
  protected readonly unidades = signal<Unidad[]>([]);
  protected readonly formulario = signal<FormularioBeneficiario | null>(null);
  protected readonly errorRut = errorRutAlEscribir;
  private guardado = false;

  constructor() {
    effect(() => {
      forkJoin([
        this.servicio.obtener(Number(this.id())),
        this.unidadesServicio.listar(),
      ]).subscribe(([b, unidades]) => {
        // Si la unidad actual está desactivada, se agrega para que el formulario la muestre.
        if (!unidades.some((u) => u.id === b.unidadId)) {
          unidades = [
            ...unidades,
            {
              id: b.unidadId,
              nombre: b.unidad,
              rama: b.rama,
              activa: false,
              beneficiarios: 0,
              patrullas: [],
              maximo: Number.MAX_SAFE_INTEGER,
              subgrupo: SUBGRUPO_GENERICO,
            },
          ];
        }
        this.unidades.set(unidades);
        this.beneficiario.set(b);
        this.formulario.set(crearFormularioBeneficiario(this.fb, this.servicio.existe, b));
      });
    });
  }

  tieneCambiosSinGuardar(): boolean {
    return !this.guardado && !!this.formulario()?.dirty;
  }

  protected guardar(): void {
    const f = this.formulario();
    const b = this.beneficiario();
    if (!f || !b || !f.valid) return;
    const datos = aSolicitudBeneficiario(f);
    const cambiaUnidad = datos.unidadId !== b.unidadId;
    const cambiaPatrulla = datos.patrullaId !== b.patrullaId;
    const confirmar$ =
      cambiaUnidad || cambiaPatrulla
        ? this.confirmacion.guardar(
            `Se registrará un cambio de ${cambiaUnidad ? 'unidad' : 'patrulla'} en el historial de ${b.nombres}.`,
          )
        : of(true);
    confirmar$
      .pipe(concatMap((ok) => (ok ? this.servicio.editar(b.id, datos) : EMPTY)))
      .subscribe(() => {
        this.guardado = true;
        this.router.navigate(['/miembros/beneficiarios', b.id]);
      });
  }
}

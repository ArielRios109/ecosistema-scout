import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatStepperIntl, MatStepperModule } from '@angular/material/stepper';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { concatMap, filter, map, Observable, of } from 'rxjs';
import { ConCambiosSinGuardar } from '../../../shared/confirmacion/cambios-sin-guardar.guard';
import { errorRutAlEscribir } from '../../../shared/rut/rut';
import { RutInput } from '../../../shared/rut/rut-input';
import {
  ApoderadoDialogo,
  DatosApoderadoDialogo,
  ResultadoApoderadoDialogo,
} from '../apoderados/apoderado-dialogo';
import {
  DatosVinculoDialogo,
  ResultadoVinculo,
  VinculoDialogo,
} from '../apoderados/vinculo-dialogo';
import { ContactoDialogo } from '../contactos/contacto-dialogo';
import { CAMPOS_FICHA } from '../ficha-medica/ficha-medica-beneficiario';
import {
  ApoderadoInscripcion,
  ContactoEmergencia,
  etiquetaRama,
  GRUPOS_SANGUINEOS,
  PREVISIONES,
  SolicitudContacto,
  SolicitudFichaMedica,
  Unidad,
} from '../miembros.models';
import { BeneficiariosService, UnidadesService } from '../miembros.service';
import {
  aSolicitudBeneficiario,
  CamposPersonales,
  CamposUbicacion,
  crearFormularioBeneficiario,
} from './formulario-beneficiario';

/** Apoderado agregado en el paso 4, con lo necesario para mostrarlo antes de guardar. */
interface ApoderadoAgregado extends ApoderadoInscripcion {
  nombre: string;
  rut: string;
}

/**
 * Inscripción de un beneficiario en pasos: RUT → datos personales → unidad → apoderados y contactos →
 * ficha médica (opcional) → resumen. Se guarda todo junto al final, en una sola operación de la API.
 */
@Component({
  selector: 'app-inscripcion',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatStepperModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatListModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    RutInput,
    CamposPersonales,
    CamposUbicacion,
  ],
  providers: [
    {
      provide: MatStepperIntl,
      useFactory: () => Object.assign(new MatStepperIntl(), { optionalLabel: 'Opcional' }),
    },
  ],
  templateUrl: './inscripcion.html',
  styleUrl: './inscripcion.scss',
})
export class Inscripcion implements ConCambiosSinGuardar {
  private readonly servicio = inject(BeneficiariosService);
  private readonly dialogo = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly formulario = crearFormularioBeneficiario(this.fb, this.servicio.existe);
  protected readonly ficha = this.fb.group(
    Object.fromEntries(
      ['prevision', 'grupoSanguineo', ...CAMPOS_FICHA.map((c) => c.clave)].map((c) => [c, ['']]),
    ),
  );
  protected readonly unidades = signal<Unidad[]>([]);
  protected readonly apoderados = signal<ApoderadoAgregado[]>([]);
  protected readonly contactos = signal<SolicitudContacto[]>([]);
  protected readonly maximoContactos = 5;
  protected readonly camposFicha = CAMPOS_FICHA;
  protected readonly grupos = GRUPOS_SANGUINEOS;
  protected readonly previsiones = PREVISIONES;
  protected readonly errorRut = errorRutAlEscribir;
  protected readonly etiquetaRama = etiquetaRama;
  private guardado = false;

  protected readonly esMovil = toSignal(
    inject(BreakpointObserver)
      .observe([Breakpoints.XSmall])
      .pipe(map((r) => r.matches)),
    { initialValue: false },
  );

  constructor() {
    inject(UnidadesService)
      .listar()
      .subscribe((u) => this.unidades.set(u));
  }

  tieneCambiosSinGuardar(): boolean {
    return (
      !this.guardado &&
      (this.formulario.dirty || this.apoderados().length > 0 || this.contactos().length > 0)
    );
  }

  protected get rut() {
    return this.formulario.controls.identificacion.controls.rut;
  }

  protected registrado(): { id: number; nombre: string } | null {
    return this.rut.getError('rutRegistrado') ?? null;
  }

  protected nombreCompleto(): string {
    const { nombres, apellidos } = this.formulario.controls.personales.getRawValue();
    return `${nombres ?? ''} ${apellidos ?? ''}`.trim() || 'el beneficiario';
  }

  protected unidadElegida(): string {
    const { unidadId, patrullaId } = this.formulario.controls.ubicacion.getRawValue();
    const unidad = this.unidades().find((u) => u.id === unidadId);
    const patrulla = unidad?.patrullas.find((p) => p.id === patrullaId);
    return unidad ? `${unidad.nombre}${patrulla ? ' · ' + patrulla.nombre : ''}` : '—';
  }

  // ----- Apoderados -----

  protected agregarApoderado(): void {
    this.dialogo
      .open<VinculoDialogo, DatosVinculoDialogo, ResultadoVinculo>(VinculoDialogo, {
        data: { nombreBeneficiario: this.nombreCompleto() },
        width: '480px',
      })
      .afterClosed()
      .pipe(
        filter((r): r is ResultadoVinculo => !!r),
        concatMap((r) => this.apoderadoDe(r)),
      )
      .subscribe((agregado) => {
        const repetido = this.apoderados().some(
          (a) =>
            (agregado.apoderadoId && a.apoderadoId === agregado.apoderadoId) ||
            a.rut === agregado.rut,
        );
        if (repetido) return;
        this.apoderados.update((lista) => {
          const otros = agregado.esPrincipal
            ? lista.map((a) => ({ ...a, esPrincipal: false }))
            : lista;
          // El primero queda como principal si no se marcó ninguno.
          return [
            ...otros,
            { ...agregado, esPrincipal: agregado.esPrincipal || lista.length === 0 },
          ];
        });
      });
  }

  /** Apoderado elegido de la lista, o uno nuevo (que se crea recién al inscribir). */
  private apoderadoDe(r: ResultadoVinculo): Observable<ApoderadoAgregado> {
    const base = { parentesco: r.parentesco, esPrincipal: r.esPrincipal };
    if (r.apoderadoId !== 'nuevo') {
      return of({
        ...base,
        apoderadoId: r.apoderadoId,
        nuevo: null,
        nombre: r.nombre ?? '',
        rut: r.rut ?? '',
      });
    }
    return this.dialogo
      .open<ApoderadoDialogo, DatosApoderadoDialogo, ResultadoApoderadoDialogo>(ApoderadoDialogo, {
        data: { permitirExistente: true },
        width: '520px',
      })
      .afterClosed()
      .pipe(
        filter((x): x is ResultadoApoderadoDialogo => !!x),
        map((x) =>
          x.tipo === 'existente'
            ? { ...base, apoderadoId: x.id, nuevo: null, nombre: x.nombre, rut: '' }
            : {
                ...base,
                apoderadoId: null,
                nuevo: x.datos,
                nombre: `${x.datos.nombres} ${x.datos.apellidos}`,
                rut: x.datos.rut,
              },
        ),
      );
  }

  protected marcarPrincipal(indice: number): void {
    this.apoderados.update((lista) => lista.map((a, i) => ({ ...a, esPrincipal: i === indice })));
  }

  protected quitarApoderado(indice: number): void {
    this.apoderados.update((lista) => {
      const quedan = lista.filter((_, i) => i !== indice);
      if (quedan.length && !quedan.some((a) => a.esPrincipal))
        quedan[0] = { ...quedan[0], esPrincipal: true };
      return quedan;
    });
  }

  // ----- Contactos -----

  protected agregarContacto(contacto?: SolicitudContacto, indice?: number): void {
    this.dialogo
      .open<ContactoDialogo, ContactoEmergencia | undefined, SolicitudContacto>(ContactoDialogo, {
        data: contacto ? { id: 0, ...contacto } : undefined,
        width: '440px',
      })
      .afterClosed()
      .pipe(filter((c): c is SolicitudContacto => !!c))
      .subscribe((c) =>
        this.contactos.update((lista) =>
          indice === undefined ? [...lista, c] : lista.map((x, i) => (i === indice ? c : x)),
        ),
      );
  }

  protected quitarContacto(indice: number): void {
    this.contactos.update((lista) => lista.filter((_, i) => i !== indice));
  }

  // ----- Guardar -----

  private fichaMedica(): SolicitudFichaMedica | null {
    const valores = Object.fromEntries(
      Object.entries(this.ficha.getRawValue()).map(([clave, texto]) => [
        clave,
        (texto as string | null)?.trim() || null,
      ]),
    ) as unknown as SolicitudFichaMedica;
    return Object.values(valores).some(Boolean) ? valores : null;
  }

  protected tieneFicha(): boolean {
    return this.fichaMedica() !== null;
  }

  protected inscribir(): void {
    if (!this.formulario.valid) {
      this.formulario.markAllAsTouched();
      return;
    }
    this.servicio
      .inscribir({
        beneficiario: aSolicitudBeneficiario(this.formulario),
        apoderados: this.apoderados().map(({ apoderadoId, nuevo, parentesco, esPrincipal }) => ({
          apoderadoId,
          nuevo,
          parentesco,
          esPrincipal,
        })),
        contactos: this.contactos(),
        fichaMedica: this.fichaMedica(),
      })
      .subscribe((creado) => {
        this.guardado = true;
        this.router.navigate(['/miembros/beneficiarios', creado.id]);
      });
  }

  protected cancelar(): void {
    this.router.navigate(['/miembros/beneficiarios']);
  }

  /** Para el botón "Ver ficha" cuando el RUT ya está inscrito: no pregunta por descartar (no hay nada que perder). */
  protected verFichaExistente(id: number): void {
    this.guardado = true;
    this.router.navigate(['/miembros/beneficiarios', id]);
  }
}

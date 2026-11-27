import { Component, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { aFechaIso, desdeFechaIso, proveerFechas } from '../../../core/i18n/fechas';
import { ConCambiosSinGuardar } from '../../../shared/confirmacion/cambios-sin-guardar.guard';
import {
  BienDetalle,
  Categoria,
  ESTADOS_BIEN,
  opciones,
  ORIGENES_BIEN,
  SolicitudBien,
} from '../inventario.models';
import { InventarioService } from '../inventario.service';

/** Registro y edición de un bien con los campos del formulario DAF-FOR-04. El código lo asigna la API. */
@Component({
  selector: 'app-bien-formulario',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatIconModule,
  ],
  providers: [proveerFechas()],
  templateUrl: './bien-formulario.html',
  styleUrl: './bien-formulario.scss',
})
export class BienFormulario implements ConCambiosSinGuardar {
  /** Id de la ruta al editar (/inventario/bienes/:id/editar); vacío al crear. */
  readonly id = input<string>();

  private readonly servicio = inject(InventarioService);
  private readonly router = inject(Router);

  protected readonly bien = signal<BienDetalle | null>(null);
  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly responsables = signal<{ id: number; nombre: string }[]>([]);
  protected readonly estados = opciones(ESTADOS_BIEN);
  protected readonly origenes = opciones(ORIGENES_BIEN);
  protected readonly hoy = new Date();
  private guardado = false;

  protected readonly formulario = inject(FormBuilder).group({
    nombre: ['', [Validators.required, Validators.maxLength(120)]],
    categoriaId: [null as number | null, Validators.required],
    cantidad: [1, [Validators.required, Validators.min(1)]],
    marca: ['', Validators.maxLength(60)],
    numeroSerie: ['', Validators.maxLength(60)],
    descripcion: ['', Validators.maxLength(1000)],
    fechaAdquisicion: [null as Date | null],
    precioCompra: [null as number | null, Validators.min(0)],
    valorEstimado: [null as number | null, Validators.min(0)],
    origen: ['propio', Validators.required],
    estado: ['bueno', Validators.required],
    ubicacion: ['', Validators.maxLength(120)],
    responsableId: [null as number | null],
  });

  constructor() {
    this.servicio.categorias().subscribe((c) => this.categorias.set(c));
    this.servicio.responsables().subscribe((r) => this.responsables.set(r));
    effect(() => {
      const id = this.id();
      if (!id) return;
      this.servicio.bien(Number(id)).subscribe((b) => {
        this.bien.set(b);
        this.formulario.reset({
          ...b,
          fechaAdquisicion: b.fechaAdquisicion ? desdeFechaIso(b.fechaAdquisicion) : null,
          marca: b.marca ?? '',
          numeroSerie: b.numeroSerie ?? '',
          descripcion: b.descripcion ?? '',
          ubicacion: b.ubicacion ?? '',
        });
      });
    });
  }

  tieneCambiosSinGuardar(): boolean {
    return !this.guardado && this.formulario.dirty;
  }

  /** Largo actual de un campo de texto, para los contadores. */
  protected largo(campo: keyof typeof this.formulario.controls): number {
    return String(this.formulario.controls[campo].value ?? '').length;
  }

  protected guardar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    const v = this.formulario.getRawValue();
    const texto = (valor: string | null) => valor?.trim() || null;
    const datos: SolicitudBien = {
      nombre: v.nombre!.trim(),
      categoriaId: v.categoriaId!,
      cantidad: v.cantidad!,
      marca: texto(v.marca),
      numeroSerie: texto(v.numeroSerie),
      descripcion: texto(v.descripcion),
      fechaAdquisicion: v.fechaAdquisicion ? aFechaIso(v.fechaAdquisicion) : null,
      precioCompra: v.precioCompra,
      valorEstimado: v.valorEstimado,
      origen: v.origen!,
      estado: v.estado!,
      ubicacion: texto(v.ubicacion),
      responsableId: v.responsableId,
    };
    const actual = this.bien();
    const guardado$ = actual
      ? this.servicio.editarBien(actual.id, datos)
      : this.servicio.crearBien(datos);
    guardado$.subscribe((b) => {
      this.guardado = true;
      this.router.navigate(['/inventario/bienes', b.id]);
    });
  }
}

import { LowerCasePipe } from '@angular/common';
import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { formatearFecha } from '../../../core/i18n/fechas';
import { Notificaciones } from '../../../core/notificaciones/notificaciones';
import { ConfirmacionService } from '../../../shared/confirmacion/confirmacion';
import { AccionesInventario } from '../acciones-inventario';
import {
  BienDetalle,
  ESTADOS_BIEN,
  ESTADOS_DEVOLUCION,
  MOTIVOS_BAJA,
  ORIGENES_BIEN,
  Prestamo,
} from '../inventario.models';
import { InventarioService } from '../inventario.service';
import { PermisosInventario } from '../permisos-inventario';

const TIPOS_FOTO = ['image/jpeg', 'image/png', 'image/webp'];
const TAMANO_MAXIMO_FOTO = 3 * 1024 * 1024;

/** Ficha de un bien: datos, foto, préstamos (con devolución) y baja. */
@Component({
  selector: 'app-bien-detalle',
  imports: [
    LowerCasePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './bien-detalle.html',
  styleUrl: './bien-detalle.scss',
})
export class BienDetalleVista {
  /** Id de la ruta /inventario/bienes/:id. */
  readonly id = input.required<string>();

  private readonly servicio = inject(InventarioService);
  private readonly acciones = inject(AccionesInventario);
  private readonly confirmacion = inject(ConfirmacionService);
  private readonly notificaciones = inject(Notificaciones);
  private readonly router = inject(Router);
  protected readonly permisos = inject(PermisosInventario);

  protected readonly bien = signal<BienDetalle | null>(null);
  /** URL temporal (blob) de la foto; se libera al cambiarla o salir de la página. */
  protected readonly urlFoto = signal<string | null>(null);
  protected readonly fecha = formatearFecha;
  /** Pesos chilenos con punto de miles: 89990 → "$89.990". */
  protected readonly pesos = (valor: number | null) =>
    valor === null ? '—' : `$${valor.toLocaleString('es-CL')}`;
  protected readonly estados = ESTADOS_BIEN;
  protected readonly origenes = ORIGENES_BIEN;
  protected readonly motivos = MOTIVOS_BAJA;
  protected readonly devoluciones = ESTADOS_DEVOLUCION;
  protected readonly columnas = [
    'salida',
    'destinatario',
    'cantidad',
    'devolucion',
    'estado',
    'acciones',
  ];

  constructor() {
    effect(() => this.cargar(Number(this.id())));
    inject(DestroyRef).onDestroy(() => this.cambiarFoto(null));
  }

  protected editar(): void {
    this.router.navigate(['/inventario/bienes', this.bien()!.id, 'editar']);
  }

  protected prestar(): void {
    this.acciones.prestar(this.bien()!).subscribe(() => this.cargar(this.bien()!.id));
  }

  protected devolver(prestamo: Prestamo): void {
    this.acciones.devolver(prestamo).subscribe(() => this.cargar(prestamo.bienId));
  }

  protected darDeBaja(): void {
    this.acciones.darDeBaja(this.bien()!).subscribe((b) => this.bien.set(b));
  }

  /** Valida en el navegador (tipo y tamaño) para avisar antes de subir; la API vuelve a validar. */
  protected subirFoto(evento: Event): void {
    const entrada = evento.target as HTMLInputElement;
    const archivo = entrada.files?.[0];
    entrada.value = '';
    if (!archivo) return;
    if (!TIPOS_FOTO.includes(archivo.type)) {
      this.notificaciones.aviso('La foto debe ser JPG, PNG o WebP.');
      return;
    }
    if (archivo.size > TAMANO_MAXIMO_FOTO) {
      this.notificaciones.aviso('La foto puede pesar hasta 3 MB.');
      return;
    }
    this.servicio.subirFoto(this.bien()!.id, archivo).subscribe((b) => {
      this.bien.set(b);
      this.cargarFoto(b);
    });
  }

  protected eliminarFoto(): void {
    this.confirmacion
      .ejecutar(
        {
          tipo: 'eliminar',
          titulo: '¿Eliminar la foto?',
          mensaje: 'Puedes subir otra cuando quieras.',
        },
        () => this.servicio.eliminarFoto(this.bien()!.id),
      )
      .subscribe((b) => {
        this.bien.set(b);
        this.cambiarFoto(null);
      });
  }

  private cargar(id: number): void {
    this.servicio.bien(id).subscribe((b) => {
      this.bien.set(b);
      this.cargarFoto(b);
    });
  }

  private cargarFoto(bien: BienDetalle): void {
    if (!bien.tieneFoto) {
      this.cambiarFoto(null);
      return;
    }
    this.servicio.foto(bien.id).subscribe({
      next: (blob) => this.cambiarFoto(URL.createObjectURL(blob)),
      error: () => this.cambiarFoto(null),
    });
  }

  private cambiarFoto(url: string | null): void {
    const anterior = this.urlFoto();
    if (anterior) URL.revokeObjectURL(anterior);
    this.urlFoto.set(url);
  }
}

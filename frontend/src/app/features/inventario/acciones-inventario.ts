import { inject, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { concatMap, filter, Observable } from 'rxjs';
import { ConfirmacionService } from '../../shared/confirmacion/confirmacion';
import { BajaDialogo, ResultadoBaja } from './bienes/baja-dialogo';
import { BienDetalle, MOTIVOS_BAJA, Prestamo, SolicitudPrestamo } from './inventario.models';
import { InventarioService } from './inventario.service';
import { DatosPrestamoDialogo, PrestamoDialogo } from './prestamos/prestamo-dialogo';
import { DevolucionDialogo, ResultadoDevolucion } from './prestamos/devolucion-dialogo';

/** Acciones que se usan desde el listado, la ficha del bien y la pestaña de préstamos. */
@Injectable({ providedIn: 'root' })
export class AccionesInventario {
  private readonly dialogo = inject(MatDialog);
  private readonly confirmacion = inject(ConfirmacionService);
  private readonly servicio = inject(InventarioService);

  prestar(bien: { id: number; nombre: string; disponible: number }): Observable<Prestamo> {
    return this.dialogo
      .open<PrestamoDialogo, DatosPrestamoDialogo, SolicitudPrestamo>(PrestamoDialogo, {
        data: { nombre: bien.nombre, disponible: bien.disponible },
        width: '560px',
      })
      .afterClosed()
      .pipe(
        filter((datos): datos is SolicitudPrestamo => !!datos),
        concatMap((datos) => this.servicio.prestar(bien.id, datos)),
      );
  }

  devolver(prestamo: Prestamo): Observable<Prestamo> {
    return this.dialogo
      .open<DevolucionDialogo, Prestamo, ResultadoDevolucion>(DevolucionDialogo, {
        data: prestamo,
        width: '440px',
      })
      .afterClosed()
      .pipe(
        filter((datos): datos is ResultadoDevolucion => !!datos),
        concatMap((datos) => this.servicio.devolver(prestamo.id, datos)),
      );
  }

  /** Pide motivo y detalle, y luego una confirmación final antes de dar de baja. */
  darDeBaja(bien: { id: number; nombre: string }): Observable<BienDetalle> {
    return this.dialogo
      .open<BajaDialogo, string, ResultadoBaja>(BajaDialogo, { data: bien.nombre, width: '460px' })
      .afterClosed()
      .pipe(
        filter((datos): datos is ResultadoBaja => !!datos),
        concatMap((datos) =>
          this.confirmacion.ejecutar(
            {
              tipo: 'eliminar',
              titulo: `¿Dar de baja ${bien.nombre}?`,
              mensaje: `Motivo: ${MOTIVOS_BAJA[datos.motivo]}. Ya no se podrá prestar ni editar; queda en el historial.`,
              confirmar: 'Dar de baja',
              icono: 'archive',
            },
            () => this.servicio.darDeBaja(bien.id, datos),
          ),
        ),
      );
  }
}

import { inject, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { concatMap, filter, Observable, of } from 'rxjs';
import { BeneficiarioDetalle, BeneficiarioResumen, SolicitudBeneficiario, SolicitudMovimiento, Unidad } from '../miembros.models';
import { BeneficiariosService, UnidadesService } from '../miembros.service';
import { BeneficiarioDialogo, DatosBeneficiarioDialogo } from './beneficiario-dialogo';
import { DatosMovimientoDialogo, MovimientoDialogo } from './movimiento-dialogo';

/** Acciones compartidas por el listado y la ficha del beneficiario: formulario, retiro y reingreso. */
@Injectable()
export class AccionesBeneficiario {
  private readonly dialogo = inject(MatDialog);
  private readonly servicio = inject(BeneficiariosService);
  private readonly unidades = inject(UnidadesService);

  /** Abre el formulario (con las unidades activas) y devuelve lo ingresado, o undefined si se cancela. */
  formulario(beneficiario?: BeneficiarioDetalle): Observable<SolicitudBeneficiario | undefined> {
    return this.unidades.listar().pipe(
      concatMap((unidades) => {
        // Si la unidad actual está desactivada, se incluye para que el formulario la muestre.
        if (beneficiario && !unidades.some((u) => u.id === beneficiario.unidadId)) {
          unidades = [
            ...unidades,
            { id: beneficiario.unidadId, nombre: beneficiario.unidad, rama: beneficiario.rama, activa: false, beneficiarios: 0, patrullas: [] },
          ];
        }
        return this.dialogo
          .open<BeneficiarioDialogo, DatosBeneficiarioDialogo, SolicitudBeneficiario>(BeneficiarioDialogo, {
            data: { beneficiario, unidades },
            width: '640px',
            maxWidth: '95vw',
          })
          .afterClosed();
      }),
    );
  }

  /** Carga el detalle, abre el formulario y guarda los cambios. */
  editar(id: number): Observable<BeneficiarioDetalle> {
    return this.servicio.obtener(id).pipe(
      concatMap((detalle) => this.formulario(detalle)),
      filter((datos): datos is SolicitudBeneficiario => !!datos),
      concatMap((datos) => this.servicio.editar(id, datos)),
    );
  }

  /** Retira a un beneficiario activo o reingresa a uno retirado. */
  cambiarEstado(beneficiario: BeneficiarioResumen): Observable<BeneficiarioDetalle> {
    const retiro = beneficiario.estado === 'activo';
    const unidades$: Observable<Unidad[] | undefined> = retiro ? of(undefined) : this.unidades.listar();
    return unidades$.pipe(
      concatMap((unidades) =>
        this.dialogo
          .open<MovimientoDialogo, DatosMovimientoDialogo, SolicitudMovimiento>(MovimientoDialogo, {
            data: { tipo: retiro ? 'retiro' : 'reingreso', beneficiario, unidades },
            width: '460px',
            maxWidth: '95vw',
          })
          .afterClosed(),
      ),
      filter((datos): datos is SolicitudMovimiento => !!datos),
      concatMap((datos) =>
        retiro ? this.servicio.retirar(beneficiario.id, datos) : this.servicio.reingresar(beneficiario.id, datos),
      ),
    );
  }
}

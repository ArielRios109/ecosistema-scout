import { inject, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { concatMap, filter, Observable, of } from 'rxjs';
import {
  BeneficiarioDetalle,
  BeneficiarioResumen,
  SolicitudMovimiento,
  Unidad,
} from '../miembros.models';
import { BeneficiariosService, UnidadesService } from '../miembros.service';
import { DatosMovimientoDialogo, MovimientoDialogo } from './movimiento-dialogo';

/** Acciones compartidas por el listado y la ficha del beneficiario: retiro y reingreso. */
@Injectable()
export class AccionesBeneficiario {
  private readonly dialogo = inject(MatDialog);
  private readonly servicio = inject(BeneficiariosService);
  private readonly unidades = inject(UnidadesService);

  /** Retira a un beneficiario activo o reingresa a uno retirado. */
  cambiarEstado(beneficiario: BeneficiarioResumen): Observable<BeneficiarioDetalle> {
    const retiro = beneficiario.estado === 'activo';
    const unidades$: Observable<Unidad[] | undefined> = retiro
      ? of(undefined)
      : this.unidades.listar();
    return unidades$.pipe(
      concatMap((unidades) =>
        this.dialogo
          .open<MovimientoDialogo, DatosMovimientoDialogo, SolicitudMovimiento>(MovimientoDialogo, {
            data: { tipo: retiro ? 'retiro' : 'reingreso', beneficiario, unidades },
            width: '460px',
          })
          .afterClosed(),
      ),
      filter((datos): datos is SolicitudMovimiento => !!datos),
      concatMap((datos) =>
        retiro
          ? this.servicio.retirar(beneficiario.id, datos)
          : this.servicio.reingresar(beneficiario.id, datos),
      ),
    );
  }
}

import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { concatMap, filter } from 'rxjs';
import { Auth } from '../../../core/auth/auth';
import { ROLES } from '../../../core/auth/permisos';
import { ConfirmacionService } from '../../../shared/confirmacion/confirmacion';
import { DatosTextoDialogo, TextoDialogo } from '../../../shared/texto-dialogo/texto-dialogo';
import { etiquetaRama, Patrulla, Unidad } from '../miembros.models';
import { UnidadesService } from '../miembros.service';
import { UnidadDialogo } from './unidad-dialogo';

/** Unidades del grupo con sus patrullas: crear, editar, activar y desactivar. */
@Component({
  selector: 'app-unidades',
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatChipsModule,
    MatSlideToggleModule,
  ],
  templateUrl: './unidades.html',
  styleUrl: './unidades.scss',
})
export class Unidades implements OnInit {
  private readonly servicio = inject(UnidadesService);
  private readonly dialogo = inject(MatDialog);
  private readonly confirmacion = inject(ConfirmacionService);

  protected readonly unidades = signal<Unidad[]>([]);
  protected readonly esAdmin = inject(Auth).tieneRol(ROLES.administrador);
  protected incluirInactivas = false;
  protected readonly etiquetaRama = etiquetaRama;

  ngOnInit(): void {
    this.cargar();
  }

  protected cargar(): void {
    this.servicio
      .listar(this.incluirInactivas)
      .subscribe((unidades) => this.unidades.set(unidades));
  }

  protected nuevaUnidad(): void {
    this.dialogo
      .open(UnidadDialogo, { width: '440px' })
      .afterClosed()
      .pipe(
        filter(Boolean),
        concatMap((datos) => this.servicio.crear(datos)),
      )
      .subscribe(() => this.cargar());
  }

  protected editarUnidad(unidad: Unidad): void {
    this.dialogo
      .open(UnidadDialogo, { width: '440px', data: unidad })
      .afterClosed()
      .pipe(
        filter(Boolean),
        concatMap((datos) => this.servicio.editar(unidad.id, datos)),
      )
      .subscribe(() => this.cargar());
  }

  protected cambiarEstadoUnidad(unidad: Unidad): void {
    const activar = !unidad.activa;
    this.confirmacion
      .ejecutar(
        {
          titulo: activar ? 'Activar unidad' : 'Desactivar unidad',
          mensaje: activar
            ? `${unidad.nombre} volverá a estar disponible.`
            : `${unidad.nombre} dejará de aparecer en los listados. Sus datos e historial se conservan.`,
          confirmar: activar ? 'Activar' : 'Desactivar',
          peligroso: !activar,
        },
        () => this.servicio.cambiarEstado(unidad.id, activar),
      )
      .subscribe(() => this.cargar());
  }

  protected nuevaPatrulla(unidad: Unidad): void {
    this.pedirNombre({
      titulo: `Nueva patrulla en ${unidad.nombre}`,
      etiqueta: 'Nombre de la patrulla',
      maximo: 80,
      confirmar: 'Crear',
    })
      .pipe(concatMap((nombre) => this.servicio.crearPatrulla(unidad.id, nombre)))
      .subscribe(() => this.cargar());
  }

  protected editarPatrulla(patrulla: Patrulla): void {
    this.pedirNombre({
      titulo: 'Editar patrulla',
      etiqueta: 'Nombre de la patrulla',
      valor: patrulla.nombre,
      maximo: 80,
    })
      .pipe(concatMap((nombre) => this.servicio.editarPatrulla(patrulla.id, nombre)))
      .subscribe(() => this.cargar());
  }

  protected cambiarEstadoPatrulla(patrulla: Patrulla): void {
    this.servicio
      .cambiarEstadoPatrulla(patrulla.id, !patrulla.activa)
      .subscribe(() => this.cargar());
  }

  private pedirNombre(datos: DatosTextoDialogo) {
    return this.dialogo
      .open<TextoDialogo, DatosTextoDialogo, string>(TextoDialogo, { width: '400px', data: datos })
      .afterClosed()
      .pipe(filter((nombre): nombre is string => !!nombre));
  }
}

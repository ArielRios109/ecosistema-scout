import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { NgTemplateOutlet } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { concatMap, debounceTime, filter, map, Observable, Subject } from 'rxjs';
import { Auth } from '../../../core/auth/auth';
import { ROLES } from '../../../core/auth/permisos';
import { ConfirmacionService } from '../../../shared/confirmacion/confirmacion';
import { PasswordTemporalDialogo } from '../../usuarios/password-temporal-dialogo';
import { ApoderadoResumen, SolicitudApoderado } from '../miembros.models';
import { ApoderadosService } from '../miembros.service';
import { ApoderadoBeneficiariosDialogo } from './apoderado-beneficiarios-dialogo';
import {
  ApoderadoDialogo,
  DatosApoderadoDialogo,
  ResultadoApoderadoDialogo,
} from './apoderado-dialogo';

/** Listado de apoderados: búsqueda, registro, edición, cuenta del Portal y eliminación (si no tiene vínculos). */
@Component({
  selector: 'app-apoderados',
  imports: [
    FormsModule,
    NgTemplateOutlet,
    MatTableModule,
    MatPaginatorModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './apoderados.html',
  styleUrls: ['../../../shared/estilos/listado.scss', './apoderados.scss'],
})
export class Apoderados implements OnInit {
  private readonly servicio = inject(ApoderadosService);
  private readonly dialogo = inject(MatDialog);
  private readonly confirmacion = inject(ConfirmacionService);
  protected readonly esAdmin = inject(Auth).tieneRol(ROLES.administrador);

  protected readonly apoderados = signal<ApoderadoResumen[]>([]);
  protected readonly total = signal(0);
  protected pagina = 1;
  protected tamano = 10;
  protected buscar = '';
  private readonly busqueda$ = new Subject<string>();

  protected readonly esMovil = toSignal(
    inject(BreakpointObserver)
      .observe([Breakpoints.XSmall])
      .pipe(map((r) => r.matches)),
    { initialValue: false },
  );
  protected readonly columnas = [
    'nombre',
    'rut',
    'contacto',
    'beneficiarios',
    'cuenta',
    'acciones',
  ];

  ngOnInit(): void {
    this.busqueda$.pipe(debounceTime(350)).subscribe(() => {
      this.pagina = 1;
      this.cargar();
    });
    this.cargar();
  }

  protected alBuscar(texto: string): void {
    this.busqueda$.next(texto);
  }

  protected alPaginar(evento: PageEvent): void {
    this.pagina = evento.pageIndex + 1;
    this.tamano = evento.pageSize;
    this.cargar();
  }

  protected nuevo(): void {
    this.formulario()
      .pipe(concatMap((datos) => this.servicio.crear(datos)))
      .subscribe(() => this.cargar());
  }

  protected editar(apoderado: ApoderadoResumen): void {
    this.formulario(apoderado)
      .pipe(concatMap((datos) => this.servicio.editar(apoderado.id, datos)))
      .subscribe(() => this.cargar());
  }

  protected verBeneficiarios(apoderado: ApoderadoResumen): void {
    this.servicio.obtener(apoderado.id).subscribe((detalle) =>
      this.dialogo.open(ApoderadoBeneficiariosDialogo, {
        data: detalle,
        width: '480px',
      }),
    );
  }

  protected crearCuenta(apoderado: ApoderadoResumen): void {
    this.confirmacion
      .ejecutar(
        {
          titulo: 'Crear cuenta del Portal',
          mensaje: `Se creará una cuenta para ${apoderado.email} con una contraseña temporal, que deberá cambiar al ingresar.`,
          confirmar: 'Crear cuenta',
        },
        () => this.servicio.crearCuenta(apoderado.id),
      )
      .subscribe((cuenta) => {
        if (cuenta.passwordTemporal) {
          this.dialogo.open(PasswordTemporalDialogo, {
            data: cuenta,
            width: '440px',
            disableClose: true,
          });
        }
        this.cargar();
      });
  }

  protected eliminar(apoderado: ApoderadoResumen): void {
    this.confirmacion
      .ejecutar(
        {
          tipo: 'eliminar',
          titulo: 'Eliminar apoderado',
          mensaje: `Se eliminarán los datos de ${apoderado.nombres} ${apoderado.apellidos}. Esta acción no se puede deshacer.`,
          confirmar: 'Eliminar',
        },
        () => this.servicio.eliminar(apoderado.id),
      )
      .subscribe(() => this.cargar());
  }

  private formulario(apoderado?: ApoderadoResumen): Observable<SolicitudApoderado> {
    return this.dialogo
      .open<ApoderadoDialogo, DatosApoderadoDialogo, ResultadoApoderadoDialogo>(ApoderadoDialogo, {
        data: { apoderado },
        width: '520px',
      })
      .afterClosed()
      .pipe(
        filter(
          (r): r is Extract<ResultadoApoderadoDialogo, { tipo: 'nuevo' }> => r?.tipo === 'nuevo',
        ),
        map((r) => r.datos),
      );
  }

  private cargar(): void {
    this.servicio
      .listar({ pagina: this.pagina, tamano: this.tamano, buscar: this.buscar })
      .subscribe((pagina) => {
        this.apoderados.set(pagina.elementos);
        this.total.set(pagina.total);
      });
  }
}

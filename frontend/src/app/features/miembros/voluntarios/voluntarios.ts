import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { NgTemplateOutlet } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { concatMap, debounceTime, filter, map, Subject } from 'rxjs';
import { ConfirmacionService } from '../../../shared/confirmacion/confirmacion';
import { SolicitudVoluntario, Unidad, VoluntarioResumen } from '../miembros.models';
import { UnidadesService, VoluntariosService } from '../miembros.service';
import { CargosDialogo } from './cargos-dialogo';
import { VoluntarioDialogo } from './voluntario-dialogo';

/** Voluntarios adultos del grupo con sus cargos vigentes; filtros por unidad y por si tienen cargo. */
@Component({
  selector: 'app-voluntarios',
  imports: [
    FormsModule,
    NgTemplateOutlet,
    MatTableModule,
    MatPaginatorModule,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './voluntarios.html',
  styleUrl: '../listado.scss',
})
export class Voluntarios implements OnInit {
  private readonly servicio = inject(VoluntariosService);
  private readonly unidadesServicio = inject(UnidadesService);
  private readonly dialogo = inject(MatDialog);
  private readonly confirmacion = inject(ConfirmacionService);

  protected readonly voluntarios = signal<VoluntarioResumen[]>([]);
  protected readonly total = signal(0);
  protected readonly unidades = signal<Unidad[]>([]);
  protected pagina = 1;
  protected tamano = 10;
  protected buscar = '';
  protected unidadId: number | null = null;
  protected conCargo: boolean | null = null;
  private readonly busqueda$ = new Subject<string>();

  protected readonly esMovil = toSignal(
    inject(BreakpointObserver)
      .observe([Breakpoints.XSmall])
      .pipe(map((r) => r.matches)),
    { initialValue: false },
  );
  protected readonly columnas = ['nombre', 'rut', 'contacto', 'cargos', 'acciones'];

  ngOnInit(): void {
    this.unidadesServicio.listar().subscribe((u) => this.unidades.set(u));
    this.busqueda$.pipe(debounceTime(350)).subscribe(() => {
      this.pagina = 1;
      this.cargar();
    });
    this.cargar();
  }

  protected alBuscar(texto: string): void {
    this.busqueda$.next(texto);
  }

  protected alFiltrar(): void {
    this.pagina = 1;
    this.cargar();
  }

  protected alPaginar(evento: PageEvent): void {
    this.pagina = evento.pageIndex + 1;
    this.tamano = evento.pageSize;
    this.cargar();
  }

  protected nuevo(): void {
    this.formulario()
      .pipe(concatMap((datos) => this.servicio.crear(datos)))
      .subscribe((creado) => {
        this.cargar();
        this.abrirCargos(creado.id);
      });
  }

  protected editar(voluntario: VoluntarioResumen): void {
    this.formulario(voluntario)
      .pipe(concatMap((datos) => this.servicio.editar(voluntario.id, datos)))
      .subscribe(() => this.cargar());
  }

  protected cargos(voluntario: VoluntarioResumen): void {
    this.abrirCargos(voluntario.id);
  }

  protected eliminar(voluntario: VoluntarioResumen): void {
    this.confirmacion
      .ejecutar(
        {
          tipo: 'eliminar',
          titulo: 'Eliminar voluntario',
          mensaje: `Se eliminarán los datos de ${voluntario.nombres} ${voluntario.apellidos}. Solo es posible si nunca tuvo cargos.`,
          confirmar: 'Eliminar',
        },
        () => this.servicio.eliminar(voluntario.id),
      )
      .subscribe(() => this.cargar());
  }

  private abrirCargos(id: number): void {
    this.servicio.obtener(id).subscribe((detalle) =>
      this.dialogo
        .open(CargosDialogo, { data: detalle, width: '600px' })
        .afterClosed()
        .subscribe(() => this.cargar()),
    );
  }

  private formulario(voluntario?: VoluntarioResumen) {
    return this.dialogo
      .open<VoluntarioDialogo, VoluntarioResumen | undefined, SolicitudVoluntario>(
        VoluntarioDialogo,
        {
          data: voluntario,
          width: '520px',
        },
      )
      .afterClosed()
      .pipe(filter((datos): datos is SolicitudVoluntario => !!datos));
  }

  private cargar(): void {
    this.servicio
      .listar({
        pagina: this.pagina,
        tamano: this.tamano,
        buscar: this.buscar,
        unidadId: this.unidadId,
        conCargo: this.conCargo,
      })
      .subscribe((pagina) => {
        this.voluntarios.set(pagina.elementos);
        this.total.set(pagina.total);
      });
  }
}

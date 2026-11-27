import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { NgTemplateOutlet } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { Router } from '@angular/router';
import { debounceTime, map, Subject } from 'rxjs';
import { BeneficiarioResumen, EstadoBeneficiario, Unidad } from '../miembros.models';
import { BeneficiariosService, UnidadesService } from '../miembros.service';
import { AccionesBeneficiario } from './acciones-beneficiario';
import { AvisosBeneficiario } from './avisos-beneficiario';

/** Listado de beneficiarios con búsqueda, filtros por unidad y estado, y paginación (tarjetas en el móvil). */
@Component({
  selector: 'app-beneficiarios',
  imports: [
    FormsModule,
    NgTemplateOutlet,
    MatTableModule,
    MatPaginatorModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    AvisosBeneficiario,
    MatMenuModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  providers: [AccionesBeneficiario],
  templateUrl: './beneficiarios.html',
  styleUrls: ['../listado.scss', './beneficiarios.scss'],
})
export class Beneficiarios implements OnInit {
  private readonly servicio = inject(BeneficiariosService);
  private readonly unidadesServicio = inject(UnidadesService);
  private readonly router = inject(Router);
  protected readonly acciones = inject(AccionesBeneficiario);

  protected readonly beneficiarios = signal<BeneficiarioResumen[]>([]);
  protected readonly total = signal(0);
  protected readonly unidades = signal<Unidad[]>([]);

  protected pagina = 1;
  protected tamano = 10;
  protected buscar = '';
  protected unidadId: number | null = null;
  protected estado: EstadoBeneficiario | null = 'activo';
  private readonly busqueda$ = new Subject<string>();

  protected readonly esMovil = toSignal(
    inject(BreakpointObserver)
      .observe([Breakpoints.XSmall])
      .pipe(map((r) => r.matches)),
    { initialValue: false },
  );
  protected readonly columnas = ['nombre', 'rut', 'edad', 'unidad', 'estado', 'acciones'];

  ngOnInit(): void {
    this.unidadesServicio.listar(true).subscribe((unidades) => this.unidades.set(unidades));
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

  protected ver(beneficiario: BeneficiarioResumen): void {
    this.router.navigate(['/miembros/beneficiarios', beneficiario.id]);
  }

  protected nuevo(): void {
    this.router.navigate(['/miembros/beneficiarios/nuevo']);
  }

  protected editar(beneficiario: BeneficiarioResumen): void {
    this.router.navigate(['/miembros/beneficiarios', beneficiario.id, 'editar']);
  }

  protected retirarOReingresar(beneficiario: BeneficiarioResumen): void {
    this.acciones.cambiarEstado(beneficiario).subscribe(() => this.cargar());
  }

  private cargar(): void {
    this.servicio
      .listar({
        pagina: this.pagina,
        tamano: this.tamano,
        buscar: this.buscar,
        unidadId: this.unidadId,
        estado: this.estado,
      })
      .subscribe((pagina) => {
        this.beneficiarios.set(pagina.elementos);
        this.total.set(pagina.total);
      });
  }
}

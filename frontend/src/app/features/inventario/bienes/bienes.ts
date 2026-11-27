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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import { debounceTime, map, Subject } from 'rxjs';
import { AccionesInventario } from '../acciones-inventario';
import { BienResumen, Categoria, ESTADOS_BIEN, opciones } from '../inventario.models';
import { FiltroBienes, InventarioService } from '../inventario.service';
import { PermisosInventario } from '../permisos-inventario';

/** Listado de bienes con búsqueda, filtros, paginación y exportación a Excel (tarjetas en el celular). */
@Component({
  selector: 'app-bienes',
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
    MatSelectModule,
    MatSlideToggleModule,
  ],
  templateUrl: './bienes.html',
  styleUrls: ['../../../shared/estilos/listado.scss', './bienes.scss'],
})
export class Bienes implements OnInit {
  private readonly servicio = inject(InventarioService);
  private readonly acciones = inject(AccionesInventario);
  private readonly router = inject(Router);
  protected readonly permisos = inject(PermisosInventario);

  protected readonly bienes = signal<BienResumen[]>([]);
  protected readonly total = signal(0);
  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly estados = opciones(ESTADOS_BIEN);
  protected readonly nombreEstado = ESTADOS_BIEN;

  protected filtro: Required<Pick<FiltroBienes, 'pagina' | 'tamano'>> & FiltroBienes = {
    pagina: 1,
    tamano: 10,
    buscar: '',
    categoriaId: null,
    estado: null,
    ubicacion: '',
    incluirBajas: false,
  };
  private readonly escritura$ = new Subject<void>();

  protected readonly esMovil = toSignal(
    inject(BreakpointObserver)
      .observe([Breakpoints.XSmall])
      .pipe(map((r) => r.matches)),
    { initialValue: false },
  );
  protected readonly columnas = [
    'codigo',
    'nombre',
    'categoria',
    'disponible',
    'estado',
    'ubicacion',
    'acciones',
  ];

  ngOnInit(): void {
    this.servicio.categorias().subscribe((c) => this.categorias.set(c));
    // Los campos de texto esperan a que se deje de escribir; los selectores filtran al tiro.
    this.escritura$.pipe(debounceTime(350)).subscribe(() => this.alFiltrar());
    this.cargar();
  }

  protected alEscribir(): void {
    this.escritura$.next();
  }

  protected alFiltrar(): void {
    this.filtro.pagina = 1;
    this.cargar();
  }

  protected alPaginar(evento: PageEvent): void {
    this.filtro.pagina = evento.pageIndex + 1;
    this.filtro.tamano = evento.pageSize;
    this.cargar();
  }

  protected ver(bien: BienResumen): void {
    this.router.navigate(['/inventario/bienes', bien.id]);
  }

  protected nuevo(): void {
    this.router.navigate(['/inventario/bienes/nuevo']);
  }

  protected editar(bien: BienResumen): void {
    this.router.navigate(['/inventario/bienes', bien.id, 'editar']);
  }

  protected prestar(bien: BienResumen): void {
    this.acciones.prestar(bien).subscribe(() => this.cargar());
  }

  protected darDeBaja(bien: BienResumen): void {
    this.acciones.darDeBaja(bien).subscribe(() => this.cargar());
  }

  /** Descarga el Excel con los mismos filtros del listado (sin paginar). */
  protected exportar(): void {
    const { pagina: _pagina, tamano: _tamano, ...filtros } = this.filtro;
    this.servicio.exportar(filtros).subscribe((archivo) => {
      const enlace = document.createElement('a');
      enlace.href = URL.createObjectURL(archivo);
      enlace.download = `inventario-${new Date().toISOString().slice(0, 10)}.xlsx`;
      enlace.click();
      URL.revokeObjectURL(enlace.href);
    });
  }

  private cargar(): void {
    this.servicio.bienes(this.filtro).subscribe((pagina) => {
      this.bienes.set(pagina.elementos);
      this.total.set(pagina.total);
    });
  }
}

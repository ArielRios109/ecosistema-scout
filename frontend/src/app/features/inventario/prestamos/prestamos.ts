import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { debounceTime, map, Subject } from 'rxjs';
import { formatearFecha } from '../../../core/i18n/fechas';
import { AccionesInventario } from '../acciones-inventario';
import { ESTADOS_DEVOLUCION, Prestamo } from '../inventario.models';
import { FiltroPrestamos, InventarioService } from '../inventario.service';
import { PermisosInventario } from '../permisos-inventario';

/** Préstamos de todo el inventario: los activos primero, con los vencidos destacados y la devolución a un clic. */
@Component({
  selector: 'app-prestamos',
  imports: [
    FormsModule,
    RouterLink,
    MatTableModule,
    MatPaginatorModule,
    MatCardModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  template: `
    <div class="filtros flex items-center gap-2 mb-2">
      <mat-form-field appearance="outline" class="flex-1 buscador" subscriptSizing="dynamic">
        <mat-label>Buscar por bien, destinatario o actividad</mat-label>
        <mat-icon matPrefix>search</mat-icon>
        <input matInput [(ngModel)]="filtro.buscar" (ngModelChange)="escritura$.next()" />
      </mat-form-field>
      <mat-button-toggle-group
        [(ngModel)]="filtro.estado"
        (change)="alFiltrar()"
        aria-label="Estado del préstamo"
      >
        <mat-button-toggle value="activos">Activos</mat-button-toggle>
        <mat-button-toggle value="vencidos">Vencidos</mat-button-toggle>
        <mat-button-toggle value="devueltos">Devueltos</mat-button-toggle>
        <mat-button-toggle [value]="null">Todos</mat-button-toggle>
      </mat-button-toggle-group>
    </div>

    @if (esMovil()) {
      <div class="flex-col gap-2">
        @for (p of prestamos(); track p.id) {
          <mat-card appearance="outlined">
            <mat-card-header>
              <mat-card-title>{{ p.cantidad }} × {{ p.bien }}</mat-card-title>
              <mat-card-subtitle
                >{{ p.destinatario }}{{ p.actividad ? ' · ' + p.actividad : '' }}</mat-card-subtitle
              >
            </mat-card-header>
            <mat-card-content class="flex items-center justify-between gap-2 mt-2">
              <span [class.vencido]="p.vencido">{{ estado(p) }}</span>
              @if (!p.fechaDevolucion && permisos.prestar) {
                <button mat-stroked-button (click)="devolver(p)">Devolver</button>
              }
            </mat-card-content>
          </mat-card>
        } @empty {
          <p class="texto-secundario texto-centro p-4">No hay préstamos que coincidan.</p>
        }
      </div>
    } @else {
      <div class="tabla-contenedor">
        <table mat-table [dataSource]="prestamos()" class="ancho-completo">
          <ng-container matColumnDef="bien">
            <th mat-header-cell *matHeaderCellDef>Bien</th>
            <td mat-cell *matCellDef="let p">
              <a [routerLink]="['/inventario/bienes', p.bienId]">{{ p.bien }}</a>
              <small class="texto-secundario bloque">{{ p.codigoBien }}</small>
            </td>
          </ng-container>
          <ng-container matColumnDef="cantidad">
            <th mat-header-cell *matHeaderCellDef>Cant.</th>
            <td mat-cell *matCellDef="let p">{{ p.cantidad }}</td>
          </ng-container>
          <ng-container matColumnDef="destinatario">
            <th mat-header-cell *matHeaderCellDef>Para</th>
            <td mat-cell *matCellDef="let p">
              {{ p.destinatario }}
              @if (p.actividad) {
                <small class="texto-secundario bloque">{{ p.actividad }}</small>
              }
            </td>
          </ng-container>
          <ng-container matColumnDef="salida">
            <th mat-header-cell *matHeaderCellDef>Salida</th>
            <td mat-cell *matCellDef="let p">{{ fecha(p.fechaSalida) }}</td>
          </ng-container>
          <ng-container matColumnDef="estado">
            <th mat-header-cell *matHeaderCellDef>Estado</th>
            <td mat-cell *matCellDef="let p" [class.vencido]="p.vencido">{{ estado(p) }}</td>
          </ng-container>
          <ng-container matColumnDef="acciones">
            <th mat-header-cell *matHeaderCellDef></th>
            <td mat-cell *matCellDef="let p" class="acciones-celda">
              @if (!p.fechaDevolucion && permisos.prestar) {
                <button mat-stroked-button (click)="devolver(p)">Devolver</button>
              }
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="columnas"></tr>
          <tr mat-row *matRowDef="let fila; columns: columnas"></tr>
          <tr class="mat-row" *matNoDataRow>
            <td class="mat-cell p-3 texto-secundario" [attr.colspan]="columnas.length">
              No hay préstamos que coincidan.
            </td>
          </tr>
        </table>
      </div>
    }

    <mat-paginator
      [length]="total()"
      [pageIndex]="filtro.pagina - 1"
      [pageSize]="filtro.tamano"
      [pageSizeOptions]="[10, 25, 50]"
      (page)="alPaginar($event)"
      aria-label="Páginas de préstamos"
    />
  `,
  styleUrl: '../../../shared/estilos/listado.scss',
  styles: `
    .vencido {
      color: var(--mat-sys-error);
      font-weight: 600;
    }
    .bloque {
      display: block;
    }
    .acciones-celda {
      text-align: end;
    }
    a {
      color: var(--mat-sys-primary);
    }
  `,
})
export class Prestamos implements OnInit {
  private readonly servicio = inject(InventarioService);
  private readonly acciones = inject(AccionesInventario);
  private readonly ruta = inject(ActivatedRoute);
  protected readonly permisos = inject(PermisosInventario);

  protected readonly prestamos = signal<Prestamo[]>([]);
  protected readonly total = signal(0);
  protected readonly fecha = formatearFecha;
  protected readonly escritura$ = new Subject<void>();
  protected filtro: Required<Pick<FiltroPrestamos, 'pagina' | 'tamano'>> & FiltroPrestamos = {
    pagina: 1,
    tamano: 10,
    buscar: '',
    estado: 'activos',
  };
  protected readonly columnas = [
    'bien',
    'cantidad',
    'destinatario',
    'salida',
    'estado',
    'acciones',
  ];
  protected readonly esMovil = toSignal(
    inject(BreakpointObserver)
      .observe([Breakpoints.XSmall])
      .pipe(map((r) => r.matches)),
    { initialValue: false },
  );

  ngOnInit(): void {
    // Desde el inicio se llega con ?estado=vencidos.
    const estado = this.ruta.snapshot.queryParamMap.get('estado');
    if (estado === 'activos' || estado === 'vencidos' || estado === 'devueltos')
      this.filtro.estado = estado;
    this.escritura$.pipe(debounceTime(350)).subscribe(() => this.alFiltrar());
    this.cargar();
  }

  protected estado(p: Prestamo): string {
    if (p.fechaDevolucion)
      return `Devuelto el ${this.fecha(p.fechaDevolucion)} · ${ESTADOS_DEVOLUCION[p.estadoDevolucion ?? '']?.toLowerCase() ?? ''}`;
    return `${p.vencido ? 'Vencido' : 'Devolver'} el ${this.fecha(p.fechaDevolucionEsperada)}`;
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

  protected devolver(prestamo: Prestamo): void {
    this.acciones.devolver(prestamo).subscribe(() => this.cargar());
  }

  private cargar(): void {
    this.servicio.prestamos(this.filtro).subscribe((pagina) => {
      this.prestamos.set(pagina.elementos);
      this.total.set(pagina.total);
    });
  }
}

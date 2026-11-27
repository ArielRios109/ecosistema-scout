import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatTooltipModule } from '@angular/material/tooltip';
import { concatMap, filter, Observable } from 'rxjs';
import { ConfirmacionService } from '../../../shared/confirmacion/confirmacion';
import { DatosTextoDialogo, TextoDialogo } from '../../../shared/texto-dialogo/texto-dialogo';
import { Categoria } from '../inventario.models';
import { InventarioService } from '../inventario.service';
import { PermisosInventario } from '../permisos-inventario';

/** Categorías de bienes. Se crean y renombran en un modal; solo se eliminan si no tienen bienes. */
@Component({
  selector: 'app-categorias',
  imports: [MatButtonModule, MatIconModule, MatListModule, MatTooltipModule],
  template: `
    @if (permisos.gestionar) {
      <div class="flex justify-end mb-2">
        <button mat-flat-button (click)="nueva()">
          <mat-icon>add</mat-icon>
          Nueva categoría
        </button>
      </div>
    }
    <mat-list class="lista">
      @for (c of categorias(); track c.id) {
        <mat-list-item>
          <mat-icon matListItemIcon>category</mat-icon>
          <span matListItemTitle>{{ c.nombre }}</span>
          <span matListItemLine>{{ c.bienes }} bien{{ c.bienes === 1 ? '' : 'es' }}</span>
          @if (permisos.gestionar) {
            <div matListItemMeta class="flex">
              <button
                mat-icon-button
                (click)="renombrar(c)"
                matTooltip="Renombrar"
                aria-label="Renombrar categoría"
              >
                <mat-icon>edit</mat-icon>
              </button>
              <button
                mat-icon-button
                (click)="eliminar(c)"
                [disabled]="c.bienes > 0"
                [matTooltip]="c.bienes > 0 ? 'Tiene bienes: no se puede eliminar' : 'Eliminar'"
                aria-label="Eliminar categoría"
              >
                <mat-icon>delete</mat-icon>
              </button>
            </div>
          }
        </mat-list-item>
      } @empty {
        <p class="texto-secundario p-3 m-0">Aún no hay categorías.</p>
      }
    </mat-list>
  `,
  styles: `
    .lista {
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 12px;
      max-width: 640px;
    }
  `,
})
export class Categorias implements OnInit {
  private readonly servicio = inject(InventarioService);
  private readonly dialogo = inject(MatDialog);
  private readonly confirmacion = inject(ConfirmacionService);
  protected readonly permisos = inject(PermisosInventario);
  protected readonly categorias = signal<Categoria[]>([]);

  ngOnInit(): void {
    this.cargar();
  }

  protected nueva(): void {
    this.pedirNombre({
      titulo: 'Nueva categoría',
      etiqueta: 'Nombre',
      maximo: 60,
      confirmar: 'Crear',
    })
      .pipe(concatMap((nombre) => this.servicio.crearCategoria(nombre)))
      .subscribe(() => this.cargar());
  }

  protected renombrar(categoria: Categoria): void {
    this.pedirNombre({
      titulo: 'Renombrar categoría',
      etiqueta: 'Nombre',
      valor: categoria.nombre,
      maximo: 60,
    })
      .pipe(concatMap((nombre) => this.servicio.editarCategoria(categoria.id, nombre)))
      .subscribe(() => this.cargar());
  }

  protected eliminar(categoria: Categoria): void {
    this.confirmacion
      .ejecutar(
        {
          tipo: 'eliminar',
          titulo: `¿Eliminar la categoría ${categoria.nombre}?`,
          mensaje: 'Esta acción no se puede deshacer.',
        },
        () => this.servicio.eliminarCategoria(categoria.id),
      )
      .subscribe(() => this.cargar());
  }

  private pedirNombre(datos: DatosTextoDialogo): Observable<string> {
    return this.dialogo
      .open<TextoDialogo, DatosTextoDialogo, string>(TextoDialogo, { data: datos, width: '400px' })
      .afterClosed()
      .pipe(filter((nombre): nombre is string => !!nombre));
  }

  private cargar(): void {
    this.servicio.categorias().subscribe((c) => this.categorias.set(c));
  }
}

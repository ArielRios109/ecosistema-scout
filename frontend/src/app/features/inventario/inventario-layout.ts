import { Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/** Estructura del módulo Inventario: título y pestañas (cada pestaña es una ruta). */
@Component({
  selector: 'app-inventario-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatTabsModule],
  template: `
    <h1 class="titulo-grande m-0">Inventario</h1>
    <p class="texto-secundario m-0 mt-1">Bienes del grupo, préstamos para actividades y bajas.</p>

    <nav mat-tab-nav-bar [tabPanel]="panel" class="mt-2 mb-3" aria-label="Secciones de Inventario">
      @for (pestana of pestanas; track pestana.ruta) {
        <a
          mat-tab-link
          [routerLink]="pestana.ruta"
          routerLinkActive
          #rla="routerLinkActive"
          [active]="rla.isActive"
        >
          {{ pestana.etiqueta }}
        </a>
      }
    </nav>
    <mat-tab-nav-panel #panel>
      <router-outlet />
    </mat-tab-nav-panel>
  `,
})
export class InventarioLayout {
  protected readonly pestanas = [
    { ruta: 'bienes', etiqueta: 'Bienes' },
    { ruta: 'prestamos', etiqueta: 'Préstamos' },
    { ruta: 'categorias', etiqueta: 'Categorías' },
  ];
}

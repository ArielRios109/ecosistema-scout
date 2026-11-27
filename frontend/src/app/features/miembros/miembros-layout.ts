import { Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/** Estructura del módulo Miembros: título y pestañas de navegación (cada pestaña es una ruta). */
@Component({
  selector: 'app-miembros-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatTabsModule],
  template: `
    <h1 class="titulo-grande m-0">Miembros</h1>
    <p class="texto-secundario m-0 mt-1">Beneficiarios, unidades, apoderados y voluntarios del grupo.</p>

    <nav mat-tab-nav-bar [tabPanel]="panel" class="mt-2 mb-3" aria-label="Secciones de Miembros">
      @for (pestana of pestanas; track pestana.ruta) {
        <a mat-tab-link [routerLink]="pestana.ruta" routerLinkActive #rla="routerLinkActive" [active]="rla.isActive">
          {{ pestana.etiqueta }}
        </a>
      }
    </nav>
    <mat-tab-nav-panel #panel>
      <router-outlet />
    </mat-tab-nav-panel>
  `,
})
export class MiembrosLayout {
  protected readonly pestanas = [
    { ruta: 'beneficiarios', etiqueta: 'Beneficiarios' },
    { ruta: 'unidades', etiqueta: 'Unidades y patrullas' },
    { ruta: 'apoderados', etiqueta: 'Apoderados' },
    { ruta: 'voluntarios', etiqueta: 'Voluntarios' },
  ];
}

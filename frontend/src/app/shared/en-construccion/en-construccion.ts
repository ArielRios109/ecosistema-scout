import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/**
 * Página provisoria para los módulos que aún no se implementan.
 * Recibe el título y el ícono desde los datos de la ruta.
 */
@Component({
  selector: 'app-en-construccion',
  imports: [MatIconModule],
  template: `
    <section class="flex-col items-center texto-centro texto-secundario p-4">
      <mat-icon class="icono texto-primario">{{ icono() }}</mat-icon>
      <h1 class="titulo-chico m-0 mt-3">{{ titulo() }}</h1>
      <p class="mt-1">Este módulo está en construcción.</p>
    </section>
  `,
  styles: `
    .icono {
      width: 64px;
      height: 64px;
      font-size: 64px;
    }
    h1 {
      color: var(--mat-sys-on-surface);
    }
  `,
})
export class EnConstruccion {
  readonly titulo = input('Módulo');
  readonly icono = input('construction');
}

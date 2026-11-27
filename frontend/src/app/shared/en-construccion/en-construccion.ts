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
    <section class="vacio">
      <mat-icon class="icono">{{ icono() }}</mat-icon>
      <h1>{{ titulo() }}</h1>
      <p>Este módulo está en construcción.</p>
    </section>
  `,
  styles: `
    .vacio {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      padding: 48px 16px;
      color: var(--mat-sys-on-surface-variant);
    }
    .icono {
      width: 64px;
      height: 64px;
      font-size: 64px;
      color: var(--mat-sys-primary);
    }
    h1 {
      margin: 16px 0 4px;
      font: var(--mat-sys-headline-small);
      color: var(--mat-sys-on-surface);
    }
  `,
})
export class EnConstruccion {
  readonly titulo = input('Módulo');
  readonly icono = input('construction');
}

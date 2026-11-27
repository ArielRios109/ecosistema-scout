import { afterNextRender, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SpinnerGlobal } from './core/carga/spinner-global';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SpinnerGlobal],
  template: '<router-outlet /><app-spinner-global />',
})
export class App {
  constructor() {
    // Color de la barra del navegador en el celular, tomado del tema (sin colores fijos en el código).
    afterNextRender(() => {
      const color = getComputedStyle(document.documentElement)
        .getPropertyValue('--mat-sys-surface-container')
        .trim();
      if (!color) return;
      let meta = document.querySelector<HTMLMetaElement>('meta[name="theme-color"]');
      if (!meta) {
        meta = document.createElement('meta');
        meta.name = 'theme-color';
        document.head.appendChild(meta);
      }
      meta.content = color;
    });
  }
}

import { Component, inject } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Carga } from './carga';

/** Spinner global: indicador de carga de Material 3 al centro, sobre un fondo semitransparente. */
@Component({
  selector: 'app-spinner-global',
  imports: [MatProgressSpinnerModule],
  template: `
    @if (carga.visible()) {
      <div class="capa centrado" role="status" aria-live="polite">
        <mat-progress-spinner mode="indeterminate" diameter="56" aria-label="Cargando" />
      </div>
    }
  `,
  styles: `
    .capa {
      position: fixed;
      inset: 0;
      z-index: 1000;
      background: color-mix(in srgb, var(--mat-sys-scrim) 24%, transparent);
    }
  `,
})
export class SpinnerGlobal {
  protected readonly carga = inject(Carga);
}

import { computed, Injectable, signal } from '@angular/core';

/** Tiempo de espera antes de mostrar el spinner, para que no "parpadee" en respuestas rápidas. */
export const RETRASO_SPINNER_MS = 300;

/**
 * Estado del spinner global (un solo indicador de carga para toda la aplicación).
 * Lo maneja el interceptor HTTP; un componente también puede usarlo para tareas largas propias.
 */
@Injectable({ providedIn: 'root' })
export class Carga {
  private readonly pendientes = signal(0);
  private readonly mostrar = signal(false);
  private temporizador: ReturnType<typeof setTimeout> | undefined;

  /** true mientras el spinner está en pantalla. */
  readonly visible = computed(() => this.mostrar() && this.pendientes() > 0);

  iniciar(): void {
    this.pendientes.update((n) => n + 1);
    if (this.pendientes() === 1) {
      clearTimeout(this.temporizador);
      this.temporizador = setTimeout(() => this.mostrar.set(true), RETRASO_SPINNER_MS);
    }
  }

  terminar(): void {
    this.pendientes.update((n) => Math.max(0, n - 1));
    if (this.pendientes() === 0) {
      clearTimeout(this.temporizador);
      this.mostrar.set(false);
    }
  }
}

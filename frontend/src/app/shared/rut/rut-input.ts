import { Directive, HostListener, inject } from '@angular/core';
import { NgControl } from '@angular/forms';
import { formatearRut } from './rut';

/**
 * Da formato automático al RUT al salir del campo: "123456785" → "12.345.678-5".
 * Uso: <input matInput formControlName="rut" appRut />
 */
@Directive({ selector: 'input[appRut]' })
export class RutInput {
  private readonly control = inject(NgControl, { optional: true });

  @HostListener('blur')
  protected alSalir(): void {
    const valor = this.control?.value as string | null;
    if (valor) {
      this.control?.control?.setValue(formatearRut(valor), { emitEvent: false });
    }
  }
}

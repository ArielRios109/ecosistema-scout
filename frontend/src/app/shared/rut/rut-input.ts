import { Directive, ElementRef, HostListener, inject } from '@angular/core';
import { NgControl } from '@angular/forms';
import { LARGO_MAXIMO_RUT, normalizarEntradaRut } from './rut';

/**
 * Campo de RUT: mientras se escribe, deja solo dígitos y una K final, limita a 9 caracteres reales
 * y agrega puntos y guion ("123456785" → "12.345.678-5").
 * Uso: <input matInput formControlName="rut" appRut />
 */
@Directive({
  selector: 'input[appRut]',
  host: { maxlength: String(LARGO_MAXIMO_RUT), autocomplete: 'off', spellcheck: 'false' },
})
export class RutInput {
  private readonly control = inject(NgControl, { optional: true });
  private readonly input = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;

  @HostListener('input')
  protected alEscribir(): void {
    const formateado = normalizarEntradaRut(this.input.value);
    if (formateado !== this.input.value) {
      this.input.value = formateado;
      this.control?.control?.setValue(formateado);
    }
  }
}

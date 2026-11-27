import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Deja solo dígitos y la K, en mayúscula. */
export function limpiarRut(rut: string | null | undefined): string {
  return (rut ?? '').replace(/[^0-9kK]/g, '').toUpperCase();
}

/** Calcula el dígito verificador (módulo 11) de un número de RUT. */
export function calcularDv(numero: number): string {
  let suma = 0;
  let multiplo = 2;
  while (numero > 0) {
    suma += (numero % 10) * multiplo;
    numero = Math.floor(numero / 10);
    multiplo = multiplo === 7 ? 2 : multiplo + 1;
  }
  const resto = 11 - (suma % 11);
  return resto === 11 ? '0' : resto === 10 ? 'K' : String(resto);
}

/** Indica si el RUT es válido (acepta cualquier formato: con o sin puntos y guion). */
export function rutValido(rut: string | null | undefined): boolean {
  const limpio = limpiarRut(rut);
  if (limpio.length < 2 || limpio.length > 9) return false;
  const numero = Number(limpio.slice(0, -1));
  if (!Number.isInteger(numero) || numero <= 0) return false;
  return calcularDv(numero) === limpio.slice(-1);
}

/** Formato para mostrar: 12.345.678-9 (también formatea RUT incompletos mientras se escriben). */
export function formatearRut(rut: string | null | undefined): string {
  const limpio = limpiarRut(rut);
  if (limpio.length < 2) return limpio;
  const cuerpo = limpio.slice(0, -1).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  return `${cuerpo}-${limpio.slice(-1)}`;
}

/** Validador de formularios reactivos: error { rut: true } si el RUT no es válido. */
export const validarRut: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  !control.value || rutValido(control.value) ? null : { rut: true };

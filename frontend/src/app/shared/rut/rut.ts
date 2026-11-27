import { ErrorStateMatcher } from '@angular/material/core';
import { AbstractControl, AsyncValidatorFn, ValidationErrors, ValidatorFn } from '@angular/forms';
import { catchError, map, Observable, of, switchMap, timer } from 'rxjs';

/** Largo máximo con formato ("12.345.678-9"), igual que en la API. */
export const LARGO_MAXIMO_RUT = 12;
/** Caracteres reales: hasta 8 dígitos más el dígito verificador. */
const CARACTERES_RUT = 9;

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

/**
 * Indica si el RUT es válido. Acepta con o sin puntos y guion, pero no otros caracteres
 * ni más de 12 caracteres (la misma regla que la API).
 */
export function rutValido(rut: string | null | undefined): boolean {
  const texto = (rut ?? '').trim();
  if (texto.length > LARGO_MAXIMO_RUT || /[^0-9kK.-]/.test(texto)) return false;
  const limpio = limpiarRut(texto);
  if (limpio.length < 2 || limpio.length > CARACTERES_RUT) return false;
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

/**
 * Limpia lo que se escribe en el campo: solo dígitos y una K al final (dígito verificador),
 * como máximo 9 caracteres reales, con puntos y guion automáticos.
 */
export function normalizarEntradaRut(texto: string | null | undefined): string {
  const limpio = limpiarRut(texto);
  const digitos = limpio.slice(0, -1).replace(/K/g, '') + limpio.slice(-1);
  return formatearRut(digitos.slice(0, CARACTERES_RUT));
}

/** Validador de formularios reactivos: error { rut: true } si el RUT no es válido. */
export const validarRut: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  !control.value || rutValido(control.value) ? null : { rut: true };

/** Respuesta de la API a "¿ya existe este RUT?" (RutRegistrado en el backend). */
export interface RutRegistrado {
  existe: boolean;
  id: number | null;
  nombre: string | null;
}

/**
 * Validador asíncrono: con un RUT válido, consulta a la API si ya está registrado y marca
 * { rutRegistrado: { id, nombre } }. `idActual` permite editar el propio registro sin marcar error.
 */
export function rutDisponible(
  consultar: (rut: string) => Observable<RutRegistrado>,
  idActual?: () => number | undefined,
): AsyncValidatorFn {
  return (control) => {
    if (!control.value || !rutValido(control.value)) return of(null);
    return timer(300).pipe(
      switchMap(() => consultar(control.value)),
      map((r) =>
        r.existe && r.id !== idActual?.()
          ? { rutRegistrado: { id: r.id, nombre: r.nombre } }
          : null,
      ),
      catchError(() => of(null)),
    );
  };
}

/**
 * Deja deshabilitados los demás campos del formulario hasta que el RUT sea válido y no esté registrado
 * (incluida la consulta a la API). Devuelve la suscripción para cancelarla al destruir el componente.
 */
export function habilitarSegunRut(
  formulario: { controls: Record<string, AbstractControl> },
  campoRut = 'rut',
) {
  const rut = formulario.controls[campoRut];
  const aplicar = () => {
    for (const [nombre, control] of Object.entries(formulario.controls)) {
      if (nombre === campoRut) continue;
      if (rut.valid) control.enable({ emitEvent: false });
      else control.disable({ emitEvent: false });
    }
  };
  aplicar();
  return rut.statusChanges.subscribe(aplicar);
}

/**
 * Muestra el error del RUT mientras se escribe (sin esperar a salir del campo), pero recién cuando ya hay
 * 8 caracteres o más, para no marcar error con el RUT a medio escribir.
 * Uso: <input matInput formControlName="rut" appRut [errorStateMatcher]="errorRutAlEscribir" />
 */
export const errorRutAlEscribir: ErrorStateMatcher = {
  isErrorState: (control) =>
    !!control &&
    control.invalid &&
    (control.touched || (control.dirty && limpiarRut(control.value).length >= 8)),
};

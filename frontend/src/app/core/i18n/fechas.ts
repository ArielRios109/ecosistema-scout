import { Injectable, Provider } from '@angular/core';
import {
  DateAdapter,
  MAT_DATE_FORMATS,
  MAT_DATE_LOCALE,
  MatDateFormats,
  NativeDateAdapter,
} from '@angular/material/core';

/**
 * Adaptador de fechas para los datepicker de Material: se escriben y muestran como dd-mm-aaaa
 * (también acepta dd/mm/aaaa) y la semana parte el lunes, como en Chile.
 */
@Injectable()
export class AdaptadorFechas extends NativeDateAdapter {
  override parse(valor: unknown): Date | null {
    if (typeof valor === 'string') {
      const partes = valor.trim().match(/^(\d{1,2})[-/.](\d{1,2})[-/.](\d{4})$/);
      if (partes) {
        const [, dia, mes, anio] = partes.map(Number);
        const fecha = new Date(anio, mes - 1, dia);
        return fecha.getMonth() === mes - 1 ? fecha : this.invalid();
      }
      return valor.trim() ? this.invalid() : null;
    }
    return super.parse(valor);
  }

  override format(fecha: Date, formato: object): string {
    if ((formato as { tipo?: string }).tipo === 'entrada') {
      return formatearFecha(aFechaIso(fecha));
    }
    return super.format(fecha, formato);
  }

  override getFirstDayOfWeek(): number {
    return 1;
  }
}

const FORMATOS: MatDateFormats = {
  parse: { dateInput: { tipo: 'entrada' } },
  display: {
    dateInput: { tipo: 'entrada' },
    monthYearLabel: { year: 'numeric', month: 'short' },
    dateA11yLabel: { year: 'numeric', month: 'long', day: 'numeric' },
    monthYearA11yLabel: { year: 'numeric', month: 'long' },
  },
};

/** Proveedores para usar datepicker en español de Chile. */
export function proveerFechas(): Provider[] {
  return [
    { provide: MAT_DATE_LOCALE, useValue: 'es-CL' },
    { provide: DateAdapter, useClass: AdaptadorFechas },
    { provide: MAT_DATE_FORMATS, useValue: FORMATOS },
  ];
}

/** Date local → "aaaa-mm-dd" (formato DateOnly de la API), sin desfase de zona horaria. */
export function aFechaIso(fecha: Date): string {
  const dos = (n: number) => String(n).padStart(2, '0');
  return `${fecha.getFullYear()}-${dos(fecha.getMonth() + 1)}-${dos(fecha.getDate())}`;
}

/** "aaaa-mm-dd" → Date local. */
export function desdeFechaIso(iso: string): Date {
  const [anio, mes, dia] = iso.split('-').map(Number);
  return new Date(anio, mes - 1, dia);
}

/** "aaaa-mm-dd" → "dd-mm-aaaa" para mostrar. */
export function formatearFecha(iso: string | null | undefined): string {
  if (!iso) return '';
  const [anio, mes, dia] = iso.slice(0, 10).split('-');
  return `${dia}-${mes}-${anio}`;
}

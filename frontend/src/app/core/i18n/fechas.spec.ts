import { TestBed } from '@angular/core/testing';
import { DateAdapter } from '@angular/material/core';
import { aFechaIso, desdeFechaIso, formatearFecha, proveerFechas } from './fechas';

describe('fechas', () => {
  let adaptador: DateAdapter<Date>;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: proveerFechas() });
    adaptador = TestBed.inject(DateAdapter);
  });

  it('interpreta dd-mm-aaaa y dd/mm/aaaa', () => {
    expect(aFechaIso(adaptador.parse('05-03-2014', {})!)).toBe('2014-03-05');
    expect(aFechaIso(adaptador.parse('5/3/2014', {})!)).toBe('2014-03-05');
  });

  it('rechaza fechas inexistentes y devuelve null si está vacío', () => {
    expect(adaptador.isValid(adaptador.parse('31-02-2014', {})!)).toBe(false);
    expect(adaptador.parse('', {})).toBeNull();
  });

  it('convierte entre ISO y el formato chileno sin desfase de zona horaria', () => {
    expect(aFechaIso(desdeFechaIso('2026-01-01'))).toBe('2026-01-01');
    expect(formatearFecha('2014-03-05')).toBe('05-03-2014');
  });
});

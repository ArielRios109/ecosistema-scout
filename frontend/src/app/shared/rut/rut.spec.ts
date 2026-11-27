import { FormControl } from '@angular/forms';
import { formatearRut, rutValido, validarRut } from './rut';

describe('RUT chileno', () => {
  it('reconoce RUT válidos en distintos formatos', () => {
    for (const rut of ['11.111.111-1', '12345678-5', '123456785', '7.775.735-k', '7775735K']) {
      expect(rutValido(rut)).toBe(true);
    }
  });

  it('rechaza RUT con dígito verificador incorrecto o vacíos', () => {
    for (const rut of ['12345678-9', '1', '', null, 'abc-d']) {
      expect(rutValido(rut)).toBe(false);
    }
  });

  it('da formato con puntos y guion', () => {
    expect(formatearRut('123456785')).toBe('12.345.678-5');
    expect(formatearRut('7775735k')).toBe('7.775.735-K');
  });

  it('el validador marca error solo cuando hay un RUT inválido', () => {
    expect(validarRut(new FormControl(''))).toBeNull();
    expect(validarRut(new FormControl('12.345.678-5'))).toBeNull();
    expect(validarRut(new FormControl('12.345.678-9'))).toEqual({ rut: true });
  });
});

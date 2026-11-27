import { FormControl } from '@angular/forms';
import { Observable, of } from 'rxjs';
import {
  formatearRut,
  normalizarEntradaRut,
  RutRegistrado,
  rutDisponible,
  rutValido,
  validarRut,
} from './rut';

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

describe('Entrada de RUT', () => {
  it('quita letras y símbolos, deja la K solo al final y limita a 9 caracteres', () => {
    expect(normalizarEntradaRut('abc12.345.678-5999xyz')).toBe('12.345.678-5');
    expect(normalizarEntradaRut('7k77k5735k')).toBe('7.775.735-K');
    expect(normalizarEntradaRut('1')).toBe('1');
    expect(normalizarEntradaRut('12')).toBe('1-2');
  });

  it('rechaza RUT con caracteres extraños o demasiado largos, como la API', () => {
    expect(rutValido('abc12.345.678-5')).toBe(false);
    expect(rutValido('0012.345.678-5')).toBe(false);
  });
});

describe('rutDisponible', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  async function validar(valor: string, respuesta: RutRegistrado, idActual?: number) {
    const consultar = vi.fn(() => of(respuesta));
    let resultado: unknown = 'pendiente';
    const validador = rutDisponible(consultar, () => idActual);
    (validador(new FormControl(valor)) as Observable<unknown>).subscribe((r) => (resultado = r));
    await vi.advanceTimersByTimeAsync(300);
    return { resultado, consultar };
  }

  it('marca error si el RUT ya está registrado por otra persona', async () => {
    const { resultado } = await validar('12.345.678-5', {
      existe: true,
      id: 4,
      nombre: 'Camila Pérez',
    });
    expect(resultado).toEqual({ rutRegistrado: { id: 4, nombre: 'Camila Pérez' } });
  });

  it('no marca error si es el mismo registro que se edita o si está libre', async () => {
    expect(
      (await validar('12.345.678-5', { existe: true, id: 4, nombre: 'X' }, 4)).resultado,
    ).toBeNull();
    expect(
      (await validar('12.345.678-5', { existe: false, id: null, nombre: null })).resultado,
    ).toBeNull();
  });

  it('no consulta a la API si el RUT no es válido', async () => {
    const { consultar, resultado } = await validar('12.345.678-9', {
      existe: true,
      id: 1,
      nombre: 'X',
    });
    expect(consultar).not.toHaveBeenCalled();
    expect(resultado).toBeNull();
  });
});

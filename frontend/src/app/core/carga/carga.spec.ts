import { TestBed } from '@angular/core/testing';
import { Carga, RETRASO_SPINNER_MS } from './carga';

describe('Carga (spinner global)', () => {
  let carga: Carga;

  beforeEach(() => {
    vi.useFakeTimers();
    carga = TestBed.inject(Carga);
  });

  afterEach(() => vi.useRealTimers());

  it('no se muestra si la petición termina antes de 300 ms', () => {
    carga.iniciar();
    vi.advanceTimersByTime(RETRASO_SPINNER_MS - 50);
    carga.terminar();
    vi.advanceTimersByTime(100);

    expect(carga.visible()).toBe(false);
  });

  it('se muestra si la petición tarda más de 300 ms y se oculta al terminar', () => {
    carga.iniciar();
    vi.advanceTimersByTime(RETRASO_SPINNER_MS + 10);
    expect(carga.visible()).toBe(true);

    carga.terminar();
    expect(carga.visible()).toBe(false);
  });

  it('sigue visible mientras quede alguna petición en curso', () => {
    carga.iniciar();
    carga.iniciar();
    vi.advanceTimersByTime(RETRASO_SPINNER_MS + 10);

    carga.terminar();
    expect(carga.visible()).toBe(true);

    carga.terminar();
    expect(carga.visible()).toBe(false);
  });
});

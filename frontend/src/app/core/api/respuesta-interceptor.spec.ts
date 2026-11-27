import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Notificaciones } from '../notificaciones/notificaciones';
import { respuestaInterceptor } from './respuesta-interceptor';

describe('respuestaInterceptor', () => {
  let http: HttpClient;
  let servidor: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([respuestaInterceptor])),
        provideHttpClientTesting(),
        { provide: Notificaciones, useValue: { exito: () => undefined, error: () => undefined } },
      ],
    });
    http = TestBed.inject(HttpClient);
    servidor = TestBed.inject(HttpTestingController);
  });

  it('entrega directamente los datos de un ResponseData', () => {
    let recibido: unknown;
    http.get('/api/prueba').subscribe((datos) => (recibido = datos));

    servidor
      .expectOne('/api/prueba')
      .flush({ exito: true, mensaje: null, datos: { nombre: 'Talí' }, errores: null });

    expect(recibido).toEqual({ nombre: 'Talí' });
  });

  it('no modifica respuestas que no son ResponseData', () => {
    let recibido: unknown;
    http.get('/otra').subscribe((datos) => (recibido = datos));

    servidor.expectOne('/otra').flush({ cualquier: 'cosa' });

    expect(recibido).toEqual({ cualquier: 'cosa' });
  });
});

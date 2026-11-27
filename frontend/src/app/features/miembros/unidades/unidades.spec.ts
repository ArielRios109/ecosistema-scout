import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Auth } from '../../../core/auth/auth';
import { Unidad } from '../miembros.models';
import { Unidades } from './unidades';

const UNIDADES: Unidad[] = [
  {
    id: 1,
    nombre: 'Tropa Lautaro',
    rama: 'scouts',
    activa: true,
    beneficiarios: 5,
    patrullas: [{ id: 10, unidadId: 1, nombre: 'Cóndores', activa: true, beneficiarios: 3 }],
  },
];

async function mostrar(roles: string[]) {
  TestBed.configureTestingModule({
    imports: [Unidades],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: Auth, useValue: { tieneRol: (rol: string) => roles.includes(rol) } },
    ],
  });
  const fixture = TestBed.createComponent(Unidades);
  fixture.detectChanges();
  const http = TestBed.inject(HttpTestingController);
  http
    .expectOne(
      (r) => r.url === '/api/miembros/unidades' && r.params.get('incluirInactivas') === 'false',
    )
    .flush(UNIDADES);
  await fixture.whenStable();
  return fixture.nativeElement as HTMLElement;
}

describe('Unidades', () => {
  it('muestra cada unidad con su rama, beneficiarios y patrullas', async () => {
    const pagina = await mostrar(['administrador']);

    expect(pagina.querySelector('mat-card-title')?.textContent).toContain('Tropa Lautaro');
    expect(pagina.querySelector('mat-card-subtitle')?.textContent).toContain('Scouts (Tropa)');
    expect(pagina.textContent).toContain('5 beneficiarios activos');
    expect(pagina.querySelector('mat-chip')?.textContent).toContain('Cóndores · 3');
  });

  it('solo el administrador puede crear unidades', async () => {
    const admin = await mostrar(['administrador']);
    expect(admin.textContent).toContain('Nueva unidad');

    TestBed.resetTestingModule();
    const dirigente = await mostrar(['dirigente']);
    expect(dirigente.textContent).not.toContain('Nueva unidad');
    expect(dirigente.textContent).toContain('Agregar patrulla');
  });
});

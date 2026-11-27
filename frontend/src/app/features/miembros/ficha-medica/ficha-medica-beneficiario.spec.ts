import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FichaMedica } from '../miembros.models';
import { FichaMedicaBeneficiario } from './ficha-medica-beneficiario';

const VACIA: FichaMedica = {
  existe: false,
  prevision: null,
  grupoSanguineo: null,
  alergias: null,
  enfermedades: null,
  medicamentos: null,
  restriccionesAlimentarias: null,
  necesidadesEmocionales: null,
  actualizadaEl: null,
  actualizadaPor: null,
  desactualizada: false,
};

async function mostrar(ficha: FichaMedica) {
  TestBed.configureTestingModule({
    imports: [FichaMedicaBeneficiario],
    providers: [provideHttpClient(), provideHttpClientTesting()],
  });
  const fixture = TestBed.createComponent(FichaMedicaBeneficiario);
  fixture.componentRef.setInput('beneficiarioId', 7);
  fixture.detectChanges();
  const http = TestBed.inject(HttpTestingController);
  http.expectOne('/api/miembros/beneficiarios/7/ficha-medica').flush(ficha);
  await fixture.whenStable();
  return { pagina: fixture.nativeElement as HTMLElement, fixture, http };
}

describe('FichaMedicaBeneficiario', () => {
  it('sin ficha, invita a completarla', async () => {
    const { pagina } = await mostrar(VACIA);

    expect(pagina.textContent).toContain('Aún no tiene ficha médica registrada');
    expect(pagina.textContent).toContain('Completar ficha');
  });

  it('con ficha, muestra los datos y quién la actualizó', async () => {
    const { pagina } = await mostrar({
      ...VACIA,
      existe: true,
      alergias: 'Penicilina',
      grupoSanguineo: 'O+',
      actualizadaEl: '2026-11-27T05:54:00Z',
      actualizadaPor: 'Administración Demo',
    });

    expect(pagina.textContent).toContain('Penicilina');
    expect(pagina.textContent).toContain('O+');
    expect(pagina.textContent).toContain('por Administración Demo');
    expect(pagina.textContent).toContain('Editar ficha');
  });

  it('al guardar envía los textos vacíos como null', async () => {
    const { pagina, fixture, http } = await mostrar(VACIA);
    (pagina.querySelector('button') as HTMLButtonElement).click();
    fixture.detectChanges();
    const componente = fixture.componentInstance as unknown as {
      formulario: { patchValue: (v: object) => void };
      guardar: () => void;
    };
    componente.formulario.patchValue({ alergias: '  Penicilina ', enfermedades: '   ' });

    componente.guardar();

    const peticion = http.expectOne((r) => r.method === 'PUT');
    expect(peticion.request.body).toEqual(
      expect.objectContaining({ alergias: 'Penicilina', enfermedades: null, prevision: null }),
    );
  });
});

import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Unidad } from '../miembros.models';
import { BeneficiarioDialogo, DatosBeneficiarioDialogo } from './beneficiario-dialogo';

const UNIDADES: Unidad[] = [
  {
    id: 1,
    nombre: 'Tropa Lautaro',
    rama: 'scouts',
    activa: true,
    beneficiarios: 0,
    patrullas: [
      { id: 10, unidadId: 1, nombre: 'Cóndores', activa: true, beneficiarios: 0 },
      { id: 11, unidadId: 1, nombre: 'Antigua', activa: false, beneficiarios: 0 },
    ],
  },
  { id: 2, nombre: 'Manada Seeonee', rama: 'lobatos', activa: true, beneficiarios: 0, patrullas: [] },
];

function crear(datos: DatosBeneficiarioDialogo = { unidades: UNIDADES }) {
  const cerrar = vi.fn();
  TestBed.configureTestingModule({
    imports: [BeneficiarioDialogo],
    providers: [
      { provide: MAT_DIALOG_DATA, useValue: datos },
      { provide: MatDialogRef, useValue: { close: cerrar } },
    ],
  });
  const fixture = TestBed.createComponent(BeneficiarioDialogo);
  fixture.detectChanges();
  // Acceso a miembros protegidos solo para la prueba.
  const componente = fixture.componentInstance as unknown as {
    formulario: {
      patchValue: (v: object) => void;
      controls: Record<string, { setValue: (v: unknown) => void; value: unknown; disabled: boolean }>;
    };
    patrullas: () => { id: number }[];
    guardar: () => void;
  };
  return { componente, cerrar };
}

describe('BeneficiarioDialogo', () => {
  it('no se cierra si el RUT no es válido', () => {
    const { componente, cerrar } = crear();
    componente.formulario.patchValue({
      rut: '12.345.678-9',
      nombres: 'Camila',
      apellidos: 'Pérez',
      fechaNacimiento: new Date(2014, 4, 10),
      unidadId: 1,
    });

    componente.guardar();

    expect(cerrar).not.toHaveBeenCalled();
  });

  it('entrega las fechas en formato ISO y solo ofrece patrullas activas de la unidad', () => {
    const { componente, cerrar } = crear();
    componente.formulario.patchValue({
      rut: '12.345.678-5',
      nombres: ' Camila ',
      apellidos: 'Pérez Soto',
      fechaNacimiento: new Date(2014, 4, 10),
      unidadId: 1,
    });
    expect(componente.patrullas().map((p) => p.id)).toEqual([10]);
    componente.formulario.controls['patrullaId'].setValue(10);
    componente.formulario.controls['fechaIngreso'].setValue(new Date(2025, 2, 1));

    componente.guardar();

    expect(cerrar).toHaveBeenCalledWith(
      expect.objectContaining({
        nombres: 'Camila',
        fechaNacimiento: '2014-05-10',
        fechaIngreso: '2025-03-01',
        unidadId: 1,
        patrullaId: 10,
        direccion: null,
      }),
    );
  });

  it('al cambiar de unidad se limpia la patrulla elegida', () => {
    const { componente } = crear();
    componente.formulario.controls['unidadId'].setValue(1);
    componente.formulario.controls['patrullaId'].setValue(10);

    componente.formulario.controls['unidadId'].setValue(2);

    expect(componente.formulario.controls['patrullaId'].value).toBeNull();
  });

  it('si el beneficiario está retirado, no permite cambiar unidad ni patrulla', () => {
    const { componente } = crear({
      unidades: UNIDADES,
      beneficiario: {
        id: 5,
        rut: '12.345.678-5',
        nombres: 'Camila',
        apellidos: 'Pérez',
        fechaNacimiento: '2014-05-10',
        edad: 12,
        direccion: null,
        unidadId: 1,
        unidad: 'Tropa Lautaro',
        rama: 'scouts',
        patrullaId: null,
        patrulla: null,
        estado: 'retirado',
        fechaIngreso: '2024-03-01',
        registroInstitucionalAnio: null,
      },
    });

    expect(componente.formulario.controls['unidadId'].disabled).toBe(true);
    expect(componente.formulario.controls['patrullaId'].disabled).toBe(true);
  });
});

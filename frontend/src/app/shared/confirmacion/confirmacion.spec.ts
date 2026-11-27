import { Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of, Subject } from 'rxjs';
import { cambiosSinGuardarGuard, ConCambiosSinGuardar } from './cambios-sin-guardar.guard';
import { completarOpciones, ConfirmacionService, protegerDialogo } from './confirmacion';

/** MatDialog falso: cada diálogo se cierra con la respuesta indicada y guarda los datos recibidos. */
function dialogoQueResponde(respuesta: boolean | undefined) {
  const abiertos: unknown[] = [];
  const falso = {
    open: (_componente: unknown, config: { data: unknown }) => {
      abiertos.push(config.data);
      return { afterClosed: () => of(respuesta) };
    },
  };
  TestBed.configureTestingModule({ providers: [{ provide: MatDialog, useValue: falso }] });
  return { servicio: TestBed.inject(ConfirmacionService), abiertos };
}

describe('ConfirmacionService', () => {
  it('cada variante trae su título, botón e icono por defecto', () => {
    expect(completarOpciones({ tipo: 'eliminar', mensaje: 'x' })).toEqual(
      expect.objectContaining({ confirmar: 'Eliminar', icono: 'delete', peligroso: true }),
    );
    expect(completarOpciones({ tipo: 'descartar', mensaje: 'x' })).toEqual(
      expect.objectContaining({
        confirmar: 'Descartar',
        cancelar: 'Seguir editando',
        peligroso: true,
      }),
    );
    expect(completarOpciones({ tipo: 'guardar', mensaje: 'x', confirmar: 'Sí, guardar' })).toEqual(
      expect.objectContaining({ confirmar: 'Sí, guardar', icono: 'save', peligroso: false }),
    );
  });

  it('eliminar() arma el título con el elemento', () => {
    const { servicio, abiertos } = dialogoQueResponde(true);
    let respuesta: boolean | undefined;

    servicio.eliminar('la patrulla Cóndores').subscribe((r) => (respuesta = r));

    expect(respuesta).toBe(true);
    expect(abiertos[0]).toEqual(
      expect.objectContaining({ titulo: '¿Eliminar la patrulla Cóndores?' }),
    );
  });

  it('si se cierra sin elegir, cuenta como "no"', () => {
    const { servicio } = dialogoQueResponde(undefined);
    let respuesta: boolean | undefined;

    servicio.confirmar({ mensaje: '¿Seguro?' }).subscribe((r) => (respuesta = r));

    expect(respuesta).toBe(false);
  });

  it('ejecutar() solo corre la acción si se confirma', () => {
    const accion = vi.fn(() => of('hecho'));
    const rechazado = dialogoQueResponde(false);
    rechazado.servicio.ejecutar({ mensaje: 'x' }, accion).subscribe();
    expect(accion).not.toHaveBeenCalled();

    TestBed.resetTestingModule();
    const aceptado = dialogoQueResponde(true);
    let resultado: string | undefined;
    aceptado.servicio.ejecutar({ mensaje: 'x' }, accion).subscribe((r) => (resultado = r));
    expect(accion).toHaveBeenCalledOnce();
    expect(resultado).toBe('hecho');
  });
});

describe('protegerDialogo', () => {
  function proteger(formularioSucio: boolean, descartar: boolean) {
    const fondo = new Subject<MouseEvent>();
    const teclas = new Subject<KeyboardEvent>();
    const ref = {
      disableClose: false,
      close: vi.fn(),
      backdropClick: () => fondo,
      keydownEvents: () => teclas,
    };
    const confirmacion = { descartarCambios: vi.fn(() => of(descartar)) };
    const formulario = new FormControl('');
    if (formularioSucio) formulario.markAsDirty();

    @Component({ template: '' })
    class Prueba {
      readonly cancelar = protegerDialogo(inject(MatDialogRef), formulario);
    }
    TestBed.configureTestingModule({
      providers: [
        { provide: MatDialogRef, useValue: ref },
        { provide: ConfirmacionService, useValue: confirmacion },
      ],
    });
    const componente = TestBed.createComponent(Prueba).componentInstance;
    return { ref, confirmacion, fondo, teclas, cancelar: componente.cancelar };
  }

  it('sin cambios, Cancelar cierra sin preguntar', () => {
    const { ref, confirmacion, cancelar } = proteger(false, true);

    cancelar();

    expect(ref.disableClose).toBe(true);
    expect(ref.close).toHaveBeenCalled();
    expect(confirmacion.descartarCambios).not.toHaveBeenCalled();
  });

  it('con cambios, Esc o el clic fuera preguntan y solo cierran si se descartan', () => {
    const descarta = proteger(true, true);
    descarta.teclas.next(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(descarta.confirmacion.descartarCambios).toHaveBeenCalled();
    expect(descarta.ref.close).toHaveBeenCalled();

    TestBed.resetTestingModule();
    const sigue = proteger(true, false);
    sigue.fondo.next(new MouseEvent('click'));
    expect(sigue.confirmacion.descartarCambios).toHaveBeenCalled();
    expect(sigue.ref.close).not.toHaveBeenCalled();
  });
});

describe('cambiosSinGuardarGuard', () => {
  function evaluar(conCambios: boolean, descartar: boolean) {
    TestBed.configureTestingModule({
      providers: [
        { provide: ConfirmacionService, useValue: { descartarCambios: () => of(descartar) } },
      ],
    });
    const pagina: ConCambiosSinGuardar = { tieneCambiosSinGuardar: () => conCambios };
    return TestBed.runInInjectionContext(() =>
      cambiosSinGuardarGuard(
        pagina,
        {} as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot,
        {} as RouterStateSnapshot,
      ),
    );
  }

  it('deja salir si no hay cambios', () => {
    expect(evaluar(false, false)).toBe(true);
  });

  it('con cambios, deja salir solo si se descartan', () => {
    let sale: boolean | undefined;
    (evaluar(true, true) as Observable<boolean>).subscribe((r) => (sale = r));
    expect(sale).toBe(true);

    TestBed.resetTestingModule();
    (evaluar(true, false) as Observable<boolean>).subscribe((r) => (sale = r));
    expect(sale).toBe(false);
  });
});

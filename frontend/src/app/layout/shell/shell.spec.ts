import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Auth, UsuarioActual } from '../../core/auth/auth';
import { Shell } from './shell';

/** Muestra el shell con un usuario de prueba y devuelve los textos del menú lateral. */
async function menuPara(roles: string[]): Promise<string[]> {
  const usuario = signal<UsuarioActual>({ id: 1, nombre: 'Prueba', email: 'prueba@demo.cl', roles, debeCambiarPassword: false });
  const authFalso = {
    usuario: computed(() => usuario()),
    autenticado: computed(() => true),
    debeCambiarPassword: computed(() => false),
    logout: () => undefined,
  };

  await TestBed.configureTestingModule({
    imports: [Shell],
    providers: [provideRouter([]), { provide: Auth, useValue: authFalso }],
  }).compileComponents();

  const fixture = TestBed.createComponent(Shell);
  await fixture.whenStable();
  const enlaces = (fixture.nativeElement as HTMLElement).querySelectorAll('mat-nav-list a [matListItemTitle]');
  return Array.from(enlaces).map((e) => e.textContent?.trim() ?? '');
}

describe('Shell: menú según el rol', () => {
  it('el administrador ve todos los módulos y Usuarios', async () => {
    expect(await menuPara(['administrador'])).toEqual([
      'Inicio',
      'Miembros',
      'Inventario',
      'Finanzas',
      'Portal de pagos',
      'Usuarios',
    ]);
  });

  it('el apoderado solo ve Inicio y el Portal de pagos', async () => {
    expect(await menuPara(['apoderado'])).toEqual(['Inicio', 'Portal de pagos']);
  });

  it('el revisor ve Inventario y Finanzas', async () => {
    expect(await menuPara(['revisor'])).toEqual(['Inicio', 'Inventario', 'Finanzas']);
  });
});

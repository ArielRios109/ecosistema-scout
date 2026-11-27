import { Modulo, puedeVerModulo } from './permisos';

describe('puedeVerModulo', () => {
  const casos: [rol: string, visibles: Modulo[]][] = [
    ['administrador', ['miembros', 'inventario', 'finanzas', 'portal']],
    ['tesoreria', ['inventario', 'finanzas', 'portal']],
    ['dirigente', ['miembros', 'inventario']],
    ['apoderado', ['portal']],
    ['revisor', ['inventario', 'finanzas']],
  ];
  const todos: Modulo[] = ['miembros', 'inventario', 'finanzas', 'portal'];

  for (const [rol, visibles] of casos) {
    it(`el rol ${rol} solo ve: ${visibles.join(', ')}`, () => {
      for (const modulo of todos) {
        expect(puedeVerModulo([rol], modulo)).toBe(visibles.includes(modulo));
      }
    });
  }

  it('suma los permisos cuando el usuario tiene varios roles', () => {
    expect(puedeVerModulo(['apoderado', 'dirigente'], 'miembros')).toBe(true);
    expect(puedeVerModulo(['apoderado', 'dirigente'], 'portal')).toBe(true);
  });

  it('sin roles no ve ningún módulo', () => {
    for (const modulo of todos) {
      expect(puedeVerModulo([], modulo)).toBe(false);
    }
  });
});

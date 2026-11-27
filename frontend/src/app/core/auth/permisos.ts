/** Roles del sistema (mismos nombres que en la API). */
export const ROLES = {
  administrador: 'administrador',
  tesoreria: 'tesoreria',
  dirigente: 'dirigente',
  apoderado: 'apoderado',
  revisor: 'revisor',
} as const;

export type Modulo = 'miembros' | 'inventario' | 'finanzas' | 'portal';

/**
 * Qué roles pueden ver cada módulo. Replica las políticas de la API
 * (backend: Modulos/Base/Autorizacion/Politicas.cs). En el frontend solo sirve
 * para mostrar u ocultar menús y rutas; la seguridad real la aplica la API.
 */
export const ROLES_POR_MODULO: Record<Modulo, readonly string[]> = {
  miembros: [ROLES.administrador, ROLES.dirigente],
  inventario: [ROLES.administrador, ROLES.tesoreria, ROLES.dirigente, ROLES.revisor],
  finanzas: [ROLES.administrador, ROLES.tesoreria, ROLES.revisor],
  portal: [ROLES.administrador, ROLES.tesoreria, ROLES.apoderado],
};

/** Nombre legible de cada rol para mostrar en pantalla. */
export const NOMBRE_ROL: Record<string, string> = {
  administrador: 'Administración',
  tesoreria: 'Tesorería',
  dirigente: 'Dirigente',
  apoderado: 'Apoderado',
  revisor: 'Comisión revisora',
};

/** Indica si alguno de los roles del usuario permite entrar al módulo. */
export function puedeVerModulo(roles: readonly string[], modulo: Modulo): boolean {
  return roles.some((rol) => ROLES_POR_MODULO[modulo].includes(rol));
}

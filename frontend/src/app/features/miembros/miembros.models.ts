/** Modelos del módulo Miembros (mismos nombres que las respuestas de la API). */

export interface Patrulla {
  id: number;
  unidadId: number;
  nombre: string;
  activa: boolean;
  beneficiarios: number;
}

export interface Unidad {
  id: number;
  nombre: string;
  rama: string;
  activa: boolean;
  beneficiarios: number;
  patrullas: Patrulla[];
}

/** Ramas scout con su nombre habitual. */
export const RAMAS: { valor: string; etiqueta: string }[] = [
  { valor: 'golondrinas', etiqueta: 'Golondrinas (Bandada)' },
  { valor: 'lobatos', etiqueta: 'Lobatos (Manada)' },
  { valor: 'guias', etiqueta: 'Guías (Compañía)' },
  { valor: 'scouts', etiqueta: 'Scouts (Tropa)' },
  { valor: 'pioneros', etiqueta: 'Pioneros (Avanzada)' },
  { valor: 'caminantes', etiqueta: 'Caminantes (Clan)' },
];

export function etiquetaRama(rama: string): string {
  return RAMAS.find((r) => r.valor === rama)?.etiqueta ?? rama;
}

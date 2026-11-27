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

export type EstadoBeneficiario = 'activo' | 'retirado';

export interface BeneficiarioResumen {
  id: number;
  rut: string;
  nombres: string;
  apellidos: string;
  fechaNacimiento: string;
  edad: number;
  unidadId: number;
  unidad: string;
  patrullaId: number | null;
  patrulla: string | null;
  estado: EstadoBeneficiario;
  fechaIngreso: string;
}

export interface BeneficiarioDetalle extends BeneficiarioResumen {
  direccion: string | null;
  rama: string;
  registroInstitucionalAnio: number | null;
}

export interface SolicitudBeneficiario {
  rut: string;
  nombres: string;
  apellidos: string;
  fechaNacimiento: string;
  direccion: string | null;
  unidadId: number;
  patrullaId: number | null;
  fechaIngreso: string | null;
  registroInstitucionalAnio: number | null;
}

export interface SolicitudMovimiento {
  fecha: string | null;
  detalle: string | null;
  unidadId?: number | null;
  patrullaId?: number | null;
}

export type TipoMovimiento = 'ingreso' | 'cambio_unidad' | 'cambio_patrulla' | 'retiro' | 'reingreso';

export interface MovimientoHistorial {
  id: number;
  tipo: TipoMovimiento;
  fecha: string;
  unidadOrigen: string | null;
  unidadDestino: string | null;
  detalle: string | null;
  usuario: string | null;
}

export const TIPOS_MOVIMIENTO: Record<TipoMovimiento, { etiqueta: string; icono: string }> = {
  ingreso: { etiqueta: 'Ingreso', icono: 'login' },
  cambio_unidad: { etiqueta: 'Cambio de unidad', icono: 'swap_horiz' },
  cambio_patrulla: { etiqueta: 'Cambio de patrulla', icono: 'groups_2' },
  retiro: { etiqueta: 'Retiro', icono: 'logout' },
  reingreso: { etiqueta: 'Reingreso', icono: 'replay' },
};

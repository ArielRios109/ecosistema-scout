/** Modelos del módulo Inventario (mismos nombres que las respuestas de la API). */

export interface Categoria {
  id: number;
  nombre: string;
  bienes: number;
}

export interface BienResumen {
  id: number;
  codigo: string;
  nombre: string;
  categoriaId: number;
  categoria: string;
  cantidad: number;
  disponible: number;
  estado: string;
  ubicacion: string | null;
  responsable: string | null;
  dadoDeBaja: boolean;
  tieneFoto: boolean;
  tienePrestamoVencido: boolean;
}

export interface Prestamo {
  id: number;
  bienId: number;
  codigoBien: string;
  bien: string;
  cantidad: number;
  destinatario: string;
  actividad: string | null;
  fechaSalida: string;
  fechaDevolucionEsperada: string;
  fechaDevolucion: string | null;
  estadoDevolucion: string | null;
  vencido: boolean;
  registradoPor: string | null;
}

export interface Baja {
  motivo: string;
  fecha: string;
  detalle: string | null;
  registradaPor: string | null;
}

export interface BienDetalle extends Omit<BienResumen, 'responsable' | 'tienePrestamoVencido'> {
  marca: string | null;
  numeroSerie: string | null;
  descripcion: string | null;
  fechaAdquisicion: string | null;
  precioCompra: number | null;
  valorEstimado: number | null;
  origen: string;
  responsableId: number | null;
  responsable: string | null;
  prestamos: Prestamo[];
  baja: Baja | null;
}

export interface SolicitudBien {
  nombre: string;
  categoriaId: number;
  cantidad: number;
  marca: string | null;
  numeroSerie: string | null;
  descripcion: string | null;
  fechaAdquisicion: string | null;
  precioCompra: number | null;
  valorEstimado: number | null;
  origen: string;
  estado: string;
  ubicacion: string | null;
  responsableId: number | null;
}

export interface SolicitudPrestamo {
  cantidad: number;
  destinatario: string;
  actividad: string | null;
  fechaSalida: string | null;
  fechaDevolucionEsperada: string;
}

/** Textos para mostrar de los valores que guarda la API. */
export const ESTADOS_BIEN: Record<string, string> = {
  bueno: 'Bueno',
  regular: 'Regular',
  malo: 'Malo',
  reparacion: 'En reparación',
};

export const ORIGENES_BIEN: Record<string, string> = {
  propio: 'Propio (compra)',
  donacion: 'Donación',
  prestamo: 'Préstamo de terceros',
  consignacion: 'Consignación',
  comodato: 'Comodato',
};

export const MOTIVOS_BAJA: Record<string, string> = {
  deterioro: 'Deterioro',
  perdida: 'Pérdida',
  robo: 'Robo',
  venta: 'Venta',
  donacion: 'Donación',
  otro: 'Otro',
};

export const ESTADOS_DEVOLUCION: Record<string, string> = {
  bueno: 'Bueno',
  regular: 'Regular',
  malo: 'Malo',
  perdido: 'Perdido',
};

/** Para recorrer los mapas anteriores en un mat-select. */
export const opciones = (mapa: Record<string, string>) =>
  Object.entries(mapa).map(([valor, etiqueta]) => ({ valor, etiqueta }));

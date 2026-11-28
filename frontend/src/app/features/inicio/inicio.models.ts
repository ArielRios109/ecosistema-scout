/** Resumen de la página de inicio (GET /api/base/inicio). Cada sección viene solo si el rol puede verla. */

export interface CupoUnidad {
  id: number;
  nombre: string;
  rama: string;
  activos: number;
  maximo: number;
}

export interface ResumenMiembros {
  /** "Todo el grupo" o el nombre de la unidad del dirigente. */
  alcance: string;
  unidadId: number | null;
  beneficiariosActivos: number;
  voluntarios: number;
  porRama: { rama: string; beneficiarios: number }[];
  unidades: CupoUnidad[];
  fichasDesactualizadas: number;
  sinFicha: number;
  egresos: number;
  sobreEdadMaxima: number;
}

export interface ResumenInventario {
  bienes: number;
  prestamosActivos: number;
  prestamosVencidos: number;
  vencidos: {
    bienId: number;
    bien: string;
    destinatario: string;
    fechaDevolucionEsperada: string;
  }[];
}

export interface BeneficiarioDeApoderado {
  id: number;
  nombre: string;
  unidad: string;
  parentesco: string;
  avisoEdad: 'egreso' | 'edad-maxima' | null;
  estadoFicha: 'al-dia' | 'desactualizada' | 'sin-ficha';
}

export interface ResumenInicio {
  miembros: ResumenMiembros | null;
  inventario: ResumenInventario | null;
  usuarios: { activos: number; conPasswordTemporal: number } | null;
  misBeneficiarios: BeneficiarioDeApoderado[] | null;
  finanzasProximamente: boolean;
}

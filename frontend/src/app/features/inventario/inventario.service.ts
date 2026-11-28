import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { aParametros, ConsultaPaginada, Pagina } from '../../core/api/pagina';
import { MOSTRAR_MENSAJE_EXITO } from '../../core/api/respuesta-interceptor';
import { SIN_NOTIFICACION_ERROR } from '../../core/auth/auth-interceptor';
import { SIN_SPINNER } from '../../core/carga/carga-interceptor';
import {
  BienDetalle,
  BienResumen,
  Categoria,
  Prestamo,
  SolicitudBien,
  SolicitudPrestamo,
} from './inventario.models';

const BASE = '/api/inventario';
const conMensaje = () => ({ context: new HttpContext().set(MOSTRAR_MENSAJE_EXITO, true) });

export interface FiltroBienes extends ConsultaPaginada {
  categoriaId?: number | null;
  estado?: string | null;
  ubicacion?: string | null;
  incluirBajas?: boolean;
}

export interface FiltroPrestamos extends ConsultaPaginada {
  estado?: 'activos' | 'vencidos' | 'devueltos' | null;
}

/** Acceso a la API de Inventario: categorías, bienes, fotos, préstamos y bajas. */
@Injectable({ providedIn: 'root' })
export class InventarioService {
  private readonly http = inject(HttpClient);

  // ----- Categorías -----

  categorias(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${BASE}/categorias`);
  }

  crearCategoria(nombre: string): Observable<Categoria> {
    return this.http.post<Categoria>(`${BASE}/categorias`, { nombre }, conMensaje());
  }

  editarCategoria(id: number, nombre: string): Observable<Categoria> {
    return this.http.put<Categoria>(`${BASE}/categorias/${id}`, { nombre }, conMensaje());
  }

  eliminarCategoria(id: number): Observable<unknown> {
    return this.http.delete(`${BASE}/categorias/${id}`, conMensaje());
  }

  // ----- Bienes -----

  bienes(filtro: FiltroBienes): Observable<Pagina<BienResumen>> {
    return this.http.get<Pagina<BienResumen>>(`${BASE}/bienes`, { params: aParametros(filtro) });
  }

  bien(id: number): Observable<BienDetalle> {
    return this.http.get<BienDetalle>(`${BASE}/bienes/${id}`);
  }

  crearBien(datos: SolicitudBien): Observable<BienDetalle> {
    return this.http.post<BienDetalle>(`${BASE}/bienes`, datos, conMensaje());
  }

  editarBien(id: number, datos: SolicitudBien): Observable<BienDetalle> {
    return this.http.put<BienDetalle>(`${BASE}/bienes/${id}`, datos, conMensaje());
  }

  darDeBaja(
    id: number,
    datos: { motivo: string; fecha: string | null; detalle: string | null },
  ): Observable<BienDetalle> {
    return this.http.post<BienDetalle>(`${BASE}/bienes/${id}/baja`, datos, conMensaje());
  }

  /** La foto se pide con el token (no sirve un <img src> directo) y se muestra como blob. */
  foto(id: number): Observable<Blob> {
    const context = new HttpContext().set(SIN_SPINNER, true).set(SIN_NOTIFICACION_ERROR, true);
    return this.http.get(`${BASE}/bienes/${id}/foto`, { responseType: 'blob', context });
  }

  subirFoto(id: number, archivo: File): Observable<BienDetalle> {
    const datos = new FormData();
    datos.append('foto', archivo);
    return this.http.put<BienDetalle>(`${BASE}/bienes/${id}/foto`, datos, conMensaje());
  }

  eliminarFoto(id: number): Observable<BienDetalle> {
    return this.http.delete<BienDetalle>(`${BASE}/bienes/${id}/foto`, conMensaje());
  }

  exportar(filtro: FiltroBienes): Observable<Blob> {
    return this.http.get(`${BASE}/bienes/exportar`, {
      params: aParametros(filtro),
      responseType: 'blob',
    });
  }

  responsables(): Observable<{ id: number; nombre: string }[]> {
    return this.http.get<{ id: number; nombre: string }[]>(`${BASE}/responsables`);
  }

  // ----- Préstamos -----

  prestamos(filtro: FiltroPrestamos): Observable<Pagina<Prestamo>> {
    return this.http.get<Pagina<Prestamo>>(`${BASE}/prestamos`, { params: aParametros(filtro) });
  }

  prestar(bienId: number, datos: SolicitudPrestamo): Observable<Prestamo> {
    return this.http.post<Prestamo>(`${BASE}/bienes/${bienId}/prestamos`, datos, conMensaje());
  }

  devolver(
    id: number,
    datos: { fechaDevolucion: string | null; estadoDevolucion: string },
  ): Observable<Prestamo> {
    return this.http.post<Prestamo>(`${BASE}/prestamos/${id}/devolucion`, datos, conMensaje());
  }
}

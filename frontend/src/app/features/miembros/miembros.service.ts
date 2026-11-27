import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MOSTRAR_MENSAJE_EXITO } from '../../core/api/respuesta-interceptor';
import { SIN_SPINNER } from '../../core/carga/carga-interceptor';
import { aParametros, ConsultaPaginada, Pagina } from '../../core/api/pagina';
import { UsuarioResumen } from '../usuarios/usuarios.service';
import {
  ApoderadoDetalle,
  ApoderadoResumen,
  BeneficiarioDetalle,
  BeneficiarioResumen,
  EstadoBeneficiario,
  MovimientoHistorial,
  Patrulla,
  SolicitudApoderado,
  SolicitudBeneficiario,
  SolicitudMovimiento,
  Unidad,
  VinculoApoderado,
} from './miembros.models';

const BASE = '/api/miembros';
/** Las operaciones que modifican datos muestran el mensaje de éxito de la API en el snackbar. */
export const conMensaje = () => ({ context: new HttpContext().set(MOSTRAR_MENSAJE_EXITO, true) });

/** Acceso a la API de unidades y patrullas. */
@Injectable({ providedIn: 'root' })
export class UnidadesService {
  private readonly http = inject(HttpClient);

  listar(incluirInactivas = false): Observable<Unidad[]> {
    const params = new HttpParams().set('incluirInactivas', incluirInactivas);
    return this.http.get<Unidad[]>(`${BASE}/unidades`, { params });
  }

  crear(datos: { nombre: string; rama: string }): Observable<Unidad> {
    return this.http.post<Unidad>(`${BASE}/unidades`, datos, conMensaje());
  }

  editar(id: number, datos: { nombre: string; rama: string }): Observable<Unidad> {
    return this.http.put<Unidad>(`${BASE}/unidades/${id}`, datos, conMensaje());
  }

  cambiarEstado(id: number, activa: boolean): Observable<Unidad> {
    return this.http.post<Unidad>(`${BASE}/unidades/${id}/${activa ? 'activar' : 'desactivar'}`, null, conMensaje());
  }

  crearPatrulla(unidadId: number, nombre: string): Observable<Patrulla> {
    return this.http.post<Patrulla>(`${BASE}/unidades/${unidadId}/patrullas`, { nombre }, conMensaje());
  }

  editarPatrulla(id: number, nombre: string): Observable<Patrulla> {
    return this.http.put<Patrulla>(`${BASE}/patrullas/${id}`, { nombre }, conMensaje());
  }

  cambiarEstadoPatrulla(id: number, activa: boolean): Observable<Patrulla> {
    return this.http.post<Patrulla>(`${BASE}/patrullas/${id}/${activa ? 'activar' : 'desactivar'}`, null, conMensaje());
  }
}

export interface FiltroBeneficiarios extends ConsultaPaginada {
  unidadId?: number | null;
  patrullaId?: number | null;
  estado?: EstadoBeneficiario | null;
}

/** Acceso a la API de beneficiarios y su historial. */
@Injectable({ providedIn: 'root' })
export class BeneficiariosService {
  private readonly http = inject(HttpClient);
  private readonly base = `${BASE}/beneficiarios`;

  listar(filtro: FiltroBeneficiarios): Observable<Pagina<BeneficiarioResumen>> {
    return this.http.get<Pagina<BeneficiarioResumen>>(this.base, { params: aParametros(filtro) });
  }

  obtener(id: number): Observable<BeneficiarioDetalle> {
    return this.http.get<BeneficiarioDetalle>(`${this.base}/${id}`);
  }

  crear(datos: SolicitudBeneficiario): Observable<BeneficiarioDetalle> {
    return this.http.post<BeneficiarioDetalle>(this.base, datos, conMensaje());
  }

  editar(id: number, datos: SolicitudBeneficiario): Observable<BeneficiarioDetalle> {
    return this.http.put<BeneficiarioDetalle>(`${this.base}/${id}`, datos, conMensaje());
  }

  retirar(id: number, datos: SolicitudMovimiento): Observable<BeneficiarioDetalle> {
    return this.http.post<BeneficiarioDetalle>(`${this.base}/${id}/retirar`, datos, conMensaje());
  }

  reingresar(id: number, datos: SolicitudMovimiento): Observable<BeneficiarioDetalle> {
    return this.http.post<BeneficiarioDetalle>(`${this.base}/${id}/reingresar`, datos, conMensaje());
  }

  historial(id: number): Observable<MovimientoHistorial[]> {
    return this.http.get<MovimientoHistorial[]>(`${this.base}/${id}/historial`);
  }
}

/** Acceso a la API de apoderados, sus vínculos con beneficiarios y su cuenta del Portal. */
@Injectable({ providedIn: 'root' })
export class ApoderadosService {
  private readonly http = inject(HttpClient);
  private readonly base = `${BASE}/apoderados`;

  listar(filtro: ConsultaPaginada, sinSpinner = false): Observable<Pagina<ApoderadoResumen>> {
    const context = sinSpinner ? new HttpContext().set(SIN_SPINNER, true) : undefined;
    return this.http.get<Pagina<ApoderadoResumen>>(this.base, { params: aParametros(filtro), context });
  }

  obtener(id: number): Observable<ApoderadoDetalle> {
    return this.http.get<ApoderadoDetalle>(`${this.base}/${id}`);
  }

  crear(datos: SolicitudApoderado): Observable<ApoderadoDetalle> {
    return this.http.post<ApoderadoDetalle>(this.base, datos, conMensaje());
  }

  editar(id: number, datos: SolicitudApoderado): Observable<ApoderadoDetalle> {
    return this.http.put<ApoderadoDetalle>(`${this.base}/${id}`, datos, conMensaje());
  }

  eliminar(id: number): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`, conMensaje());
  }

  crearCuenta(id: number): Observable<CuentaPortal> {
    return this.http.post<CuentaPortal>(`${this.base}/${id}/cuenta`, null, conMensaje());
  }

  vinculos(beneficiarioId: number): Observable<VinculoApoderado[]> {
    return this.http.get<VinculoApoderado[]>(`${BASE}/beneficiarios/${beneficiarioId}/apoderados`);
  }

  vincular(beneficiarioId: number, datos: { apoderadoId: number; parentesco: string; esPrincipal: boolean }) {
    return this.http.post<VinculoApoderado[]>(`${BASE}/beneficiarios/${beneficiarioId}/apoderados`, datos, conMensaje());
  }

  editarVinculo(beneficiarioId: number, apoderadoId: number, datos: { parentesco: string; esPrincipal: boolean }) {
    return this.http.put<VinculoApoderado[]>(`${BASE}/beneficiarios/${beneficiarioId}/apoderados/${apoderadoId}`, datos, conMensaje());
  }

  desvincular(beneficiarioId: number, apoderadoId: number) {
    return this.http.delete<VinculoApoderado[]>(`${BASE}/beneficiarios/${beneficiarioId}/apoderados/${apoderadoId}`, conMensaje());
  }
}

/** Cuenta del Portal: la contraseña temporal solo viene si se creó una cuenta nueva. */
export interface CuentaPortal {
  usuario: UsuarioResumen;
  passwordTemporal: string | null;
}

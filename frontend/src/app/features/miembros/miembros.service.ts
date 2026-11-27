import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MOSTRAR_MENSAJE_EXITO } from '../../core/api/respuesta-interceptor';
import { SIN_SPINNER } from '../../core/carga/carga-interceptor';
import { SIN_NOTIFICACION_ERROR } from '../../core/auth/auth-interceptor';
import { RutRegistrado } from '../../shared/rut/rut';
import { aParametros, ConsultaPaginada, Pagina } from '../../core/api/pagina';
import { UsuarioResumen } from '../usuarios/usuarios.service';
import {
  ApoderadoDetalle,
  ApoderadoResumen,
  BeneficiarioDetalle,
  BeneficiarioResumen,
  ContactoEmergencia,
  EstadoBeneficiario,
  FichaMedica,
  MovimientoHistorial,
  Patrulla,
  SolicitudApoderado,
  SolicitudBeneficiario,
  SolicitudContacto,
  SolicitudFichaMedica,
  SolicitudCargo,
  SolicitudMovimiento,
  SolicitudVoluntario,
  Unidad,
  VinculoApoderado,
  VoluntarioDetalle,
  VoluntarioResumen,
} from './miembros.models';

const BASE = '/api/miembros';
/** Las operaciones que modifican datos muestran el mensaje de éxito de la API en el snackbar. */
export const conMensaje = () => ({ context: new HttpContext().set(MOSTRAR_MENSAJE_EXITO, true) });

/** Consulta silenciosa (sin spinner ni snackbar) de si un RUT ya está registrado en un recurso de Miembros. */
function consultarRut(http: HttpClient, recurso: string, rut: string): Observable<RutRegistrado> {
  const context = new HttpContext().set(SIN_SPINNER, true).set(SIN_NOTIFICACION_ERROR, true);
  return http.get<RutRegistrado>(`${BASE}/${recurso}/existe`, { params: { rut }, context });
}

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
    return this.http.post<Unidad>(
      `${BASE}/unidades/${id}/${activa ? 'activar' : 'desactivar'}`,
      null,
      conMensaje(),
    );
  }

  crearPatrulla(unidadId: number, nombre: string): Observable<Patrulla> {
    return this.http.post<Patrulla>(
      `${BASE}/unidades/${unidadId}/patrullas`,
      { nombre },
      conMensaje(),
    );
  }

  editarPatrulla(id: number, nombre: string): Observable<Patrulla> {
    return this.http.put<Patrulla>(`${BASE}/patrullas/${id}`, { nombre }, conMensaje());
  }

  cambiarEstadoPatrulla(id: number, activa: boolean): Observable<Patrulla> {
    return this.http.post<Patrulla>(
      `${BASE}/patrullas/${id}/${activa ? 'activar' : 'desactivar'}`,
      null,
      conMensaje(),
    );
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

  existe = (rut: string): Observable<RutRegistrado> =>
    consultarRut(this.http, 'beneficiarios', rut);
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
    return this.http.post<BeneficiarioDetalle>(
      `${this.base}/${id}/reingresar`,
      datos,
      conMensaje(),
    );
  }

  historial(id: number): Observable<MovimientoHistorial[]> {
    return this.http.get<MovimientoHistorial[]>(`${this.base}/${id}/historial`);
  }
}

/** Acceso a la API de apoderados, sus vínculos con beneficiarios y su cuenta del Portal. */
@Injectable({ providedIn: 'root' })
export class ApoderadosService {
  private readonly http = inject(HttpClient);

  existe = (rut: string): Observable<RutRegistrado> => consultarRut(this.http, 'apoderados', rut);
  private readonly base = `${BASE}/apoderados`;

  listar(filtro: ConsultaPaginada, sinSpinner = false): Observable<Pagina<ApoderadoResumen>> {
    const context = sinSpinner ? new HttpContext().set(SIN_SPINNER, true) : undefined;
    return this.http.get<Pagina<ApoderadoResumen>>(this.base, {
      params: aParametros(filtro),
      context,
    });
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

  vincular(
    beneficiarioId: number,
    datos: { apoderadoId: number; parentesco: string; esPrincipal: boolean },
  ) {
    return this.http.post<VinculoApoderado[]>(
      `${BASE}/beneficiarios/${beneficiarioId}/apoderados`,
      datos,
      conMensaje(),
    );
  }

  editarVinculo(
    beneficiarioId: number,
    apoderadoId: number,
    datos: { parentesco: string; esPrincipal: boolean },
  ) {
    return this.http.put<VinculoApoderado[]>(
      `${BASE}/beneficiarios/${beneficiarioId}/apoderados/${apoderadoId}`,
      datos,
      conMensaje(),
    );
  }

  desvincular(beneficiarioId: number, apoderadoId: number) {
    return this.http.delete<VinculoApoderado[]>(
      `${BASE}/beneficiarios/${beneficiarioId}/apoderados/${apoderadoId}`,
      conMensaje(),
    );
  }
}

/** Cuenta del Portal: la contraseña temporal solo viene si se creó una cuenta nueva. */
export interface CuentaPortal {
  usuario: UsuarioResumen;
  passwordTemporal: string | null;
}

/** Contactos de emergencia de un beneficiario. */
@Injectable({ providedIn: 'root' })
export class ContactosService {
  private readonly http = inject(HttpClient);
  private ruta = (beneficiarioId: number) => `${BASE}/beneficiarios/${beneficiarioId}/contactos`;

  listar(beneficiarioId: number): Observable<ContactoEmergencia[]> {
    return this.http.get<ContactoEmergencia[]>(this.ruta(beneficiarioId));
  }

  crear(beneficiarioId: number, datos: SolicitudContacto): Observable<ContactoEmergencia[]> {
    return this.http.post<ContactoEmergencia[]>(this.ruta(beneficiarioId), datos, conMensaje());
  }

  editar(
    beneficiarioId: number,
    id: number,
    datos: SolicitudContacto,
  ): Observable<ContactoEmergencia[]> {
    return this.http.put<ContactoEmergencia[]>(
      `${this.ruta(beneficiarioId)}/${id}`,
      datos,
      conMensaje(),
    );
  }

  eliminar(beneficiarioId: number, id: number): Observable<ContactoEmergencia[]> {
    return this.http.delete<ContactoEmergencia[]>(
      `${this.ruta(beneficiarioId)}/${id}`,
      conMensaje(),
    );
  }
}

/** Ficha médica del beneficiario (la API la guarda cifrada). */
@Injectable({ providedIn: 'root' })
export class FichaMedicaService {
  private readonly http = inject(HttpClient);
  private ruta = (beneficiarioId: number) => `${BASE}/beneficiarios/${beneficiarioId}/ficha-medica`;

  obtener(beneficiarioId: number): Observable<FichaMedica> {
    return this.http.get<FichaMedica>(this.ruta(beneficiarioId));
  }

  guardar(beneficiarioId: number, datos: SolicitudFichaMedica): Observable<FichaMedica> {
    return this.http.put<FichaMedica>(this.ruta(beneficiarioId), datos, conMensaje());
  }
}

export interface FiltroVoluntarios extends ConsultaPaginada {
  unidadId?: number | null;
  conCargo?: boolean | null;
}

/** Voluntarios adultos y sus cargos. */
@Injectable({ providedIn: 'root' })
export class VoluntariosService {
  private readonly http = inject(HttpClient);

  existe = (rut: string): Observable<RutRegistrado> => consultarRut(this.http, 'voluntarios', rut);
  private readonly base = `${BASE}/voluntarios`;

  listar(filtro: FiltroVoluntarios): Observable<Pagina<VoluntarioResumen>> {
    return this.http.get<Pagina<VoluntarioResumen>>(this.base, { params: aParametros(filtro) });
  }

  obtener(id: number): Observable<VoluntarioDetalle> {
    return this.http.get<VoluntarioDetalle>(`${this.base}/${id}`);
  }

  crear(datos: SolicitudVoluntario): Observable<VoluntarioDetalle> {
    return this.http.post<VoluntarioDetalle>(this.base, datos, conMensaje());
  }

  editar(id: number, datos: SolicitudVoluntario): Observable<VoluntarioDetalle> {
    return this.http.put<VoluntarioDetalle>(`${this.base}/${id}`, datos, conMensaje());
  }

  eliminar(id: number): Observable<unknown> {
    return this.http.delete(`${this.base}/${id}`, conMensaje());
  }

  agregarCargo(id: number, datos: SolicitudCargo): Observable<VoluntarioDetalle> {
    return this.http.post<VoluntarioDetalle>(`${this.base}/${id}/cargos`, datos, conMensaje());
  }

  terminarCargo(
    id: number,
    cargoId: number,
    fechaTermino: string | null,
  ): Observable<VoluntarioDetalle> {
    return this.http.post<VoluntarioDetalle>(
      `${this.base}/${id}/cargos/${cargoId}/terminar`,
      { fechaTermino },
      conMensaje(),
    );
  }

  eliminarCargo(id: number, cargoId: number): Observable<VoluntarioDetalle> {
    return this.http.delete<VoluntarioDetalle>(
      `${this.base}/${id}/cargos/${cargoId}`,
      conMensaje(),
    );
  }
}

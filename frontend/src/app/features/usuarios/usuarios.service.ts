import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { aParametros, ConsultaPaginada, Pagina } from '../../core/api/pagina';
import { MOSTRAR_MENSAJE_EXITO } from '../../core/api/respuesta-interceptor';

export interface UsuarioResumen {
  id: number;
  nombre: string;
  email: string;
  roles: string[];
  activo: boolean;
  debeCambiarPassword: boolean;
  ultimoAcceso: string | null;
}

export interface UsuarioConPasswordTemporal {
  usuario: UsuarioResumen;
  passwordTemporal: string;
}

export interface RolInfo {
  nombre: string;
  descripcion: string | null;
}

export interface FiltroUsuarios extends ConsultaPaginada {
  rol?: string | null;
  activo?: boolean | null;
}

const BASE = '/api/base/usuarios';
const conMensaje = () => ({ context: new HttpContext().set(MOSTRAR_MENSAJE_EXITO, true) });

/** Acceso a la API de usuarios y roles (solo administración). */
@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);

  listar(filtro: FiltroUsuarios): Observable<Pagina<UsuarioResumen>> {
    return this.http.get<Pagina<UsuarioResumen>>(BASE, { params: aParametros(filtro) });
  }

  roles(): Observable<RolInfo[]> {
    return this.http.get<RolInfo[]>('/api/base/roles');
  }

  crear(datos: { nombre: string; email: string; roles: string[] }): Observable<UsuarioConPasswordTemporal> {
    return this.http.post<UsuarioConPasswordTemporal>(BASE, datos, conMensaje());
  }

  editar(id: number, datos: { nombre: string; email: string }): Observable<UsuarioResumen> {
    return this.http.put<UsuarioResumen>(`${BASE}/${id}`, datos);
  }

  asignarRoles(id: number, roles: string[]): Observable<UsuarioResumen> {
    return this.http.put<UsuarioResumen>(`${BASE}/${id}/roles`, { roles }, conMensaje());
  }

  cambiarEstado(id: number, activo: boolean): Observable<UsuarioResumen> {
    return this.http.post<UsuarioResumen>(`${BASE}/${id}/${activo ? 'activar' : 'desactivar'}`, null, conMensaje());
  }

  restablecerPassword(id: number): Observable<UsuarioConPasswordTemporal> {
    return this.http.post<UsuarioConPasswordTemporal>(`${BASE}/${id}/restablecer-password`, null, conMensaje());
  }
}

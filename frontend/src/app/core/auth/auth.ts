import { HttpClient, HttpContext } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { MOSTRAR_MENSAJE_EXITO } from '../api/respuesta-interceptor';

export interface UsuarioActual {
  id: number;
  nombre: string;
  email: string;
  roles: string[];
  /** true si ingresó con una contraseña temporal y debe cambiarla antes de seguir. */
  debeCambiarPassword: boolean;
}

interface RespuestaLogin {
  token: string;
  expira: string;
  usuario: UsuarioActual;
}

type SesionGuardada = RespuestaLogin;

const CLAVE_SESION = 'ecosistema-scout.sesion';

/** Maneja el inicio y cierre de sesión, el cambio de contraseña y guarda el token JWT en el navegador. */
@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly sesion = signal<SesionGuardada | null>(this.leerSesion());

  readonly usuario = computed(() => this.sesion()?.usuario ?? null);
  readonly autenticado = computed(() => this.sesion() !== null);
  readonly debeCambiarPassword = computed(() => this.sesion()?.usuario.debeCambiarPassword ?? false);

  get token(): string | null {
    return this.sesion()?.token ?? null;
  }

  login(email: string, password: string): Observable<RespuestaLogin> {
    const context = new HttpContext().set(MOSTRAR_MENSAJE_EXITO, true); // "Bienvenido, ..." en el snackbar
    return this.http
      .post<RespuestaLogin>('/api/auth/login', { email, password }, { context })
      .pipe(tap((respuesta) => this.guardar(respuesta)));
  }

  /** Cambia la contraseña del usuario actual; la API entrega un token nuevo sin la marca de cambio pendiente. */
  cambiarPassword(passwordActual: string, passwordNueva: string): Observable<RespuestaLogin> {
    const context = new HttpContext().set(MOSTRAR_MENSAJE_EXITO, true);
    return this.http
      .post<RespuestaLogin>('/api/auth/cambiar-password', { passwordActual, passwordNueva }, { context })
      .pipe(tap((respuesta) => this.guardar(respuesta)));
  }

  logout(): void {
    localStorage.removeItem(CLAVE_SESION);
    this.sesion.set(null);
    this.router.navigate(['/login']);
  }

  tieneRol(rol: string): boolean {
    return this.usuario()?.roles.includes(rol) ?? false;
  }

  private guardar(respuesta: RespuestaLogin): void {
    const sesion: SesionGuardada = { token: respuesta.token, expira: respuesta.expira, usuario: respuesta.usuario };
    localStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
    this.sesion.set(sesion);
  }

  private leerSesion(): SesionGuardada | null {
    const texto = localStorage.getItem(CLAVE_SESION);
    if (!texto) {
      return null;
    }
    try {
      const sesion = JSON.parse(texto) as SesionGuardada;
      if (new Date(sesion.expira) <= new Date()) {
        localStorage.removeItem(CLAVE_SESION);
        return null;
      }
      return sesion;
    } catch {
      localStorage.removeItem(CLAVE_SESION);
      return null;
    }
  }
}

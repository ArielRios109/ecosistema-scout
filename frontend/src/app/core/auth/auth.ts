import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

export interface UsuarioActual {
  id: number;
  nombre: string;
  email: string;
  roles: string[];
}

interface RespuestaLogin {
  token: string;
  expira: string;
  usuario: UsuarioActual;
}

interface SesionGuardada {
  token: string;
  expira: string;
  usuario: UsuarioActual;
}

const CLAVE_SESION = 'ecosistema-scout.sesion';

/** Maneja el inicio y cierre de sesión y guarda el token JWT en el navegador. */
@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly sesion = signal<SesionGuardada | null>(this.leerSesion());

  readonly usuario = computed(() => this.sesion()?.usuario ?? null);
  readonly autenticado = computed(() => this.sesion() !== null);

  get token(): string | null {
    return this.sesion()?.token ?? null;
  }

  login(email: string, password: string): Observable<RespuestaLogin> {
    return this.http.post<RespuestaLogin>('/api/auth/login', { email, password }).pipe(
      tap((respuesta) => {
        const sesion: SesionGuardada = {
          token: respuesta.token,
          expira: respuesta.expira,
          usuario: respuesta.usuario,
        };
        localStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
        this.sesion.set(sesion);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(CLAVE_SESION);
    this.sesion.set(null);
    this.router.navigate(['/login']);
  }

  tieneRol(rol: string): boolean {
    return this.usuario()?.roles.includes(rol) ?? false;
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

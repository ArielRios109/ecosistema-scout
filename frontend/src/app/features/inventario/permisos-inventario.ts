import { inject, Injectable } from '@angular/core';
import { Auth } from '../../core/auth/auth';
import { ROLES } from '../../core/auth/permisos';

/**
 * Qué puede hacer la persona en Inventario (igual que las políticas de la API): administración y tesorería
 * administran bienes, categorías y bajas; el dirigente además registra préstamos; el revisor solo consulta.
 */
@Injectable({ providedIn: 'root' })
export class PermisosInventario {
  private readonly auth = inject(Auth);

  get gestionar(): boolean {
    return this.auth.tieneRol(ROLES.administrador) || this.auth.tieneRol(ROLES.tesoreria);
  }

  get prestar(): boolean {
    return this.gestionar || this.auth.tieneRol(ROLES.dirigente);
  }
}

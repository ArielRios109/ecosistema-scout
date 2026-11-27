import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { Auth } from '../../core/auth/auth';

interface EstadoSalud {
  api: string;
  baseDatos: string;
  fecha: string;
}

type Estado = 'cargando' | 'ok' | 'error';

/** Página de inicio: saludo y estado de conexión con la API y la base de datos. */
@Component({
  selector: 'app-inicio',
  imports: [MatCardModule, MatIconModule, MatButtonModule],
  templateUrl: './inicio.html',
  styleUrl: './inicio.scss',
})
export class Inicio implements OnInit {
  private readonly http = inject(HttpClient);
  protected readonly auth = inject(Auth);

  protected readonly api = signal<Estado>('cargando');
  protected readonly baseDatos = signal<Estado>('cargando');

  ngOnInit(): void {
    this.verificar();
  }

  protected icono(estado: Estado): string {
    return { cargando: 'hourglass_empty', ok: 'check_circle', error: 'error' }[estado];
  }

  protected texto(estado: Estado): string {
    return { cargando: 'verificando…', ok: 'funcionando', error: 'sin conexión' }[estado];
  }

  protected verificar(): void {
    this.api.set('cargando');
    this.baseDatos.set('cargando');

    this.http.get<EstadoSalud>('/api/health').subscribe({
      next: (estado) => {
        this.api.set(estado.api === 'ok' ? 'ok' : 'error');
        this.baseDatos.set(estado.baseDatos === 'ok' ? 'ok' : 'error');
      },
      error: (e: HttpErrorResponse) => {
        // 503: la API responde pero la base de datos no está disponible.
        this.api.set(e.status === 503 ? 'ok' : 'error');
        this.baseDatos.set('error');
      },
    });
  }
}

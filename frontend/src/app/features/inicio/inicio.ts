import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/auth/auth';
import { Modulo, puedeVerModulo } from '../../core/auth/permisos';
import { formatearFecha } from '../../core/i18n/fechas';
import { etiquetaRama } from '../miembros/miembros.models';
import { ResumenInicio } from './inicio.models';

/** Alerta del inicio: cuántos casos hay y, si la persona puede entrar al módulo, el enlace al listado ya filtrado. */
interface Alerta {
  cantidad: number;
  texto: string;
  icono: string;
  grave: boolean;
  enlace: string;
  parametros?: Record<string, string | number>;
  modulo: Modulo;
}

/** Inicio con un resumen según el rol: cifras, cupos por unidad, alertas con acceso directo y, para apoderados, sus beneficiarios. */
@Component({
  selector: 'app-inicio',
  imports: [RouterLink, MatCardModule, MatIconModule, MatButtonModule, MatProgressBarModule],
  templateUrl: './inicio.html',
  styleUrl: './inicio.scss',
})
export class Inicio implements OnInit {
  private readonly http = inject(HttpClient);
  protected readonly auth = inject(Auth);

  protected readonly resumen = signal<ResumenInicio | null>(null);
  protected readonly etiquetaRama = etiquetaRama;
  protected readonly fecha = formatearFecha;
  /** Cupo ocupado desde el cual una unidad se marca como "cerca del máximo" (igual que la API). */
  protected readonly umbralCupo = 28;

  protected readonly alertas = computed<Alerta[]>(() => {
    const r = this.resumen();
    if (!r) return [];
    const unidad: Record<string, number> = r.miembros?.unidadId
      ? { unidadId: r.miembros.unidadId }
      : {};
    const alertas: Alerta[] = [];
    const m = r.miembros;
    if (m) {
      const cercaDelCupo = m.unidades.filter((u) => u.activos >= this.umbralCupo).length;
      alertas.push(
        {
          cantidad: m.sobreEdadMaxima,
          texto:
            m.sobreEdadMaxima === 1 ? 'supera la edad máxima (21)' : 'superan la edad máxima (21)',
          icono: 'block',
          grave: true,
          enlace: '/miembros/beneficiarios',
          parametros: { aviso: 'edad-maxima', ...unidad },
          modulo: 'miembros',
        },
        {
          cantidad: m.egresos,
          texto: 'en edad de egreso (18 a 20 años)',
          icono: 'school',
          grave: false,
          enlace: '/miembros/beneficiarios',
          parametros: { aviso: 'egreso', ...unidad },
          modulo: 'miembros',
        },
        {
          cantidad: m.fichasDesactualizadas,
          texto:
            m.fichasDesactualizadas === 1
              ? 'ficha médica con más de 3 meses'
              : 'fichas médicas con más de 3 meses',
          icono: 'medical_information',
          grave: false,
          enlace: '/miembros/beneficiarios',
          parametros: { aviso: 'ficha-desactualizada', ...unidad },
          modulo: 'miembros',
        },
        {
          cantidad: cercaDelCupo,
          texto: `${cercaDelCupo === 1 ? 'unidad cerca' : 'unidades cerca'} del cupo (${this.umbralCupo} o más de 32)`,
          icono: 'groups',
          grave: false,
          enlace: '/miembros/unidades',
          modulo: 'miembros',
        },
      );
    }
    if (r.inventario) {
      alertas.push({
        cantidad: r.inventario.prestamosVencidos,
        texto: r.inventario.prestamosVencidos === 1 ? 'préstamo vencido' : 'préstamos vencidos',
        icono: 'schedule',
        grave: true,
        enlace: '/inventario/prestamos',
        parametros: { estado: 'vencidos' },
        modulo: 'inventario',
      });
    }
    return alertas.filter((a) => a.cantidad > 0);
  });

  /** Nada que mostrar: por ejemplo, un rol sin módulos listos todavía. */
  protected readonly vacio = computed(() => {
    const r = this.resumen();
    return (
      !!r &&
      !r.miembros &&
      !r.inventario &&
      !r.usuarios &&
      !r.misBeneficiarios?.length &&
      !r.finanzasProximamente
    );
  });

  ngOnInit(): void {
    this.http.get<ResumenInicio>('/api/base/inicio').subscribe((r) => this.resumen.set(r));
  }

  protected puedeEntrar(modulo: Modulo): boolean {
    return puedeVerModulo(this.auth.usuario()?.roles ?? [], modulo);
  }

  protected porcentaje(activos: number, maximo: number): number {
    return Math.min(100, Math.round((activos / maximo) * 100));
  }
}

import { Component, effect, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterLink } from '@angular/router';
import { formatearFecha } from '../../../core/i18n/fechas';
import { BeneficiarioDetalle as Detalle, etiquetaRama, MovimientoHistorial, TIPOS_MOVIMIENTO } from '../miembros.models';
import { BeneficiariosService } from '../miembros.service';
import { AccionesBeneficiario } from './acciones-beneficiario';

/** Ficha del beneficiario: datos, apoderados, contactos, ficha médica e historial de movimientos. */
@Component({
  selector: 'app-beneficiario-detalle',
  imports: [RouterLink, MatButtonModule, MatCardModule, MatIconModule, MatTabsModule],
  providers: [AccionesBeneficiario],
  templateUrl: './beneficiario-detalle.html',
  styleUrl: './beneficiario-detalle.scss',
})
export class BeneficiarioDetalle {
  /** Id de la ruta /miembros/beneficiarios/:id. */
  readonly id = input.required<string>();

  private readonly servicio = inject(BeneficiariosService);
  private readonly acciones = inject(AccionesBeneficiario);

  protected readonly beneficiario = signal<Detalle | null>(null);
  protected readonly historial = signal<MovimientoHistorial[]>([]);
  protected readonly fecha = formatearFecha;
  protected readonly etiquetaRama = etiquetaRama;
  protected readonly tipos = TIPOS_MOVIMIENTO;

  constructor() {
    effect(() => this.cargar(Number(this.id())));
  }

  protected editar(): void {
    const actual = this.beneficiario();
    if (actual) this.acciones.editar(actual.id).subscribe(() => this.cargar(actual.id));
  }

  protected cambiarEstado(): void {
    const actual = this.beneficiario();
    if (actual) this.acciones.cambiarEstado(actual).subscribe(() => this.cargar(actual.id));
  }

  private cargar(id: number): void {
    this.servicio.obtener(id).subscribe((b) => this.beneficiario.set(b));
    this.servicio.historial(id).subscribe((h) => this.historial.set(h));
  }
}

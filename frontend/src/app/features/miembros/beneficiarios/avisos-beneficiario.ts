import { Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BeneficiarioResumen } from '../miembros.models';

interface Aviso {
  icono: string;
  texto: string;
  grave: boolean;
}

/**
 * Avisos del PGS sobre un beneficiario: edad de egreso (18), edad máxima (21) y ficha médica pendiente.
 * En el listado se ven como íconos con tooltip; en la ficha, como una franja con el texto completo
 * ("sin ficha médica" solo se muestra en la ficha, para no llenar el listado de íconos).
 */
@Component({
  selector: 'app-avisos-beneficiario',
  imports: [MatIconModule, MatTooltipModule],
  template: `
    @if (modo() === 'iconos') {
      @for (a of avisos(); track a.icono) {
        <mat-icon
          class="icono"
          [class.grave]="a.grave"
          [matTooltip]="a.texto"
          [attr.aria-label]="a.texto"
        >
          {{ a.icono }}
        </mat-icon>
      }
    } @else {
      @for (a of avisos(); track a.icono) {
        <p class="franja flex items-center gap-2 m-0 mb-2 p-3" [class.grave]="a.grave">
          <mat-icon>{{ a.icono }}</mat-icon>
          {{ a.texto }}
        </p>
      }
    }
  `,
  styles: `
    :host {
      display: contents;
    }
    .icono {
      width: 18px;
      height: 18px;
      font-size: 18px;
      margin-left: 6px;
      vertical-align: middle;
      color: var(--mat-sys-tertiary);
      &.grave {
        color: var(--mat-sys-error);
      }
    }
    .franja {
      border-radius: 12px;
      background-color: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
      &.grave {
        background-color: var(--mat-sys-error-container);
        color: var(--mat-sys-on-error-container);
      }
    }
  `,
})
export class AvisosBeneficiario {
  readonly beneficiario =
    input.required<Pick<BeneficiarioResumen, 'avisoEdad' | 'estadoFicha' | 'estado'>>();
  readonly modo = input<'iconos' | 'franja'>('iconos');

  protected readonly avisos = computed<Aviso[]>(() => {
    const b = this.beneficiario();
    if (b.estado !== 'activo') return [];
    const avisos: Aviso[] = [];
    if (b.avisoEdad === 'edad-maxima') {
      avisos.push({
        icono: 'block',
        texto: 'Tiene 21 años o más: ya superó la edad para participar.',
        grave: true,
      });
    } else if (b.avisoEdad === 'egreso') {
      avisos.push({
        icono: 'school',
        texto: 'Cumplió 18 años: corresponde preparar su egreso.',
        grave: false,
      });
    }
    if (b.estadoFicha === 'desactualizada') {
      avisos.push({
        icono: 'medical_information',
        texto: 'La ficha médica tiene más de 3 meses sin actualizar.',
        grave: false,
      });
    } else if (b.estadoFicha === 'sin-ficha' && this.modo() === 'franja') {
      avisos.push({
        icono: 'medical_information',
        texto: 'Aún no tiene ficha médica.',
        grave: false,
      });
    }
    return avisos;
  });
}

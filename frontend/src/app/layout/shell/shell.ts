import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { Auth } from '../../core/auth/auth';

interface EnlaceMenu {
  ruta: string;
  etiqueta: string;
  icono: string;
}

/** Estructura común de la aplicación: barra superior, menú lateral con los módulos y área de contenido. */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  protected readonly auth = inject(Auth);

  protected readonly esMovil = toSignal(
    inject(BreakpointObserver)
      .observe([Breakpoints.XSmall, Breakpoints.Small])
      .pipe(map((r) => r.matches)),
    { initialValue: false },
  );

  protected readonly modoMenu = computed(() => (this.esMovil() ? 'over' : 'side'));

  protected readonly enlaces: EnlaceMenu[] = [
    { ruta: '/inicio', etiqueta: 'Inicio', icono: 'home' },
    { ruta: '/miembros', etiqueta: 'Miembros', icono: 'groups' },
    { ruta: '/inventario', etiqueta: 'Inventario', icono: 'inventory_2' },
    { ruta: '/finanzas', etiqueta: 'Finanzas', icono: 'account_balance_wallet' },
    { ruta: '/portal', etiqueta: 'Portal de pagos', icono: 'payments' },
  ];
}

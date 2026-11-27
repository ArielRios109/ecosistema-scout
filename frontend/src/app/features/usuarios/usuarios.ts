import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { concatMap, debounceTime, filter, map, of, Subject } from 'rxjs';
import { NOMBRE_ROL } from '../../core/auth/permisos';
import { confirmar } from '../../shared/confirmar/confirmar-dialogo';
import { PasswordTemporalDialogo } from './password-temporal-dialogo';
import { ResultadoUsuarioDialogo, UsuarioDialogo } from './usuario-dialogo';
import { RolInfo, UsuarioConPasswordTemporal, UsuarioResumen, UsuariosService } from './usuarios.service';

/** Administración de usuarios: listado con búsqueda y filtros, creación, roles, estado y contraseñas temporales. */
@Component({
  selector: 'app-usuarios',
  imports: [
    DatePipe,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './usuarios.html',
  styleUrl: './usuarios.scss',
})
export class Usuarios implements OnInit {
  private readonly servicio = inject(UsuariosService);
  private readonly dialogo = inject(MatDialog);

  protected readonly usuarios = signal<UsuarioResumen[]>([]);
  protected readonly total = signal(0);
  protected readonly roles = signal<RolInfo[]>([]);

  protected pagina = 1;
  protected tamano = 10;
  protected buscar = '';
  protected rol: string | null = null;
  protected activo: boolean | null = null;
  private readonly busqueda$ = new Subject<string>();

  private readonly esMovil = toSignal(
    inject(BreakpointObserver).observe([Breakpoints.XSmall]).pipe(map((r) => r.matches)),
    { initialValue: false },
  );
  protected readonly columnas = computed(() =>
    this.esMovil() ? ['nombre', 'estado', 'acciones'] : ['nombre', 'email', 'roles', 'estado', 'ultimoAcceso', 'acciones'],
  );

  ngOnInit(): void {
    this.servicio.roles().subscribe((roles) => this.roles.set(roles));
    this.busqueda$.pipe(debounceTime(350)).subscribe(() => {
      this.pagina = 1;
      this.cargar();
    });
    this.cargar();
  }

  protected nombreRol(rol: string): string {
    return NOMBRE_ROL[rol] ?? rol;
  }

  protected alBuscar(texto: string): void {
    this.busqueda$.next(texto);
  }

  protected alFiltrar(): void {
    this.pagina = 1;
    this.cargar();
  }

  protected alPaginar(evento: PageEvent): void {
    this.pagina = evento.pageIndex + 1;
    this.tamano = evento.pageSize;
    this.cargar();
  }

  protected nuevo(): void {
    this.abrirFormulario()
      .pipe(concatMap((datos) => this.servicio.crear(datos)))
      .subscribe((creado) => {
        this.mostrarPassword(creado);
        this.cargar();
      });
  }

  protected editar(usuario: UsuarioResumen): void {
    this.abrirFormulario(usuario)
      .pipe(
        concatMap((datos) =>
          this.servicio.editar(usuario.id, { nombre: datos.nombre, email: datos.email }).pipe(
            concatMap(() => this.servicio.asignarRoles(usuario.id, datos.roles)),
          ),
        ),
      )
      .subscribe(() => this.cargar());
  }

  protected cambiarEstado(usuario: UsuarioResumen): void {
    const activar = !usuario.activo;
    confirmar(this.dialogo, {
      titulo: activar ? 'Activar usuario' : 'Desactivar usuario',
      mensaje: activar
        ? `${usuario.nombre} podrá volver a ingresar a Talí.`
        : `${usuario.nombre} ya no podrá ingresar a Talí. Sus datos y su historial se conservan.`,
      confirmar: activar ? 'Activar' : 'Desactivar',
      peligroso: !activar,
    })
      .pipe(
        filter(Boolean),
        concatMap(() => this.servicio.cambiarEstado(usuario.id, activar)),
      )
      .subscribe(() => this.cargar());
  }

  protected restablecer(usuario: UsuarioResumen): void {
    confirmar(this.dialogo, {
      titulo: 'Restablecer contraseña',
      mensaje: `Se generará una contraseña temporal para ${usuario.nombre}, que deberá cambiarla al ingresar.`,
      confirmar: 'Restablecer',
    })
      .pipe(
        filter(Boolean),
        concatMap(() => this.servicio.restablecerPassword(usuario.id)),
      )
      .subscribe((resultado) => {
        this.mostrarPassword(resultado);
        this.cargar();
      });
  }

  private abrirFormulario(usuario?: UsuarioResumen) {
    return this.dialogo
      .open<UsuarioDialogo, unknown, ResultadoUsuarioDialogo>(UsuarioDialogo, {
        data: { usuario, roles: this.roles() },
        width: '480px',
      })
      .afterClosed()
      .pipe(concatMap((datos) => (datos ? of(datos) : of())));
  }

  private mostrarPassword(datos: UsuarioConPasswordTemporal): void {
    this.dialogo.open(PasswordTemporalDialogo, { data: datos, width: '440px', disableClose: true });
  }

  private cargar(): void {
    this.servicio
      .listar({ pagina: this.pagina, tamano: this.tamano, buscar: this.buscar, rol: this.rol, activo: this.activo })
      .subscribe((pagina) => {
        this.usuarios.set(pagina.elementos);
        this.total.set(pagina.total);
      });
  }
}

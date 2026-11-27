import { afterNextRender, Component, DestroyRef, ElementRef, inject, signal, viewChildren } from '@angular/core';

interface Escena {
  nombre: string;
  lugar: string;
  region: string;
}

/**
 * Paisajes de Chile (videos de Pexels, licencia Pexels; créditos en el README).
 * Cada uno está recortado a 8 s en dos versiones: 1080p para computador y 720p para celular.
 */
const ESCENAS: Escena[] = [
  { nombre: 'torres-del-paine', lugar: 'Torres del Paine', region: 'Región de Magallanes y de la Antártica Chilena' },
  { nombre: 'atacama', lugar: 'Desierto de Atacama', region: 'Región de Antofagasta' },
  { nombre: 'volcan-osorno', lugar: 'Volcán Osorno', region: 'Región de Los Lagos' },
  { nombre: 'costa-algarrobo', lugar: 'Costa de Algarrobo', region: 'Región de Valparaíso' },
];

const CARPETA = 'fondos/login/';
/** Tiempo que se muestra cada paisaje antes de pasar al siguiente. */
const DURACION_ESCENA_MS = 7500;
/** Cuánto antes del cambio se empieza a descargar el siguiente video. */
const ANTICIPO_PRECARGA_MS = 3000;

/**
 * Fondo de video de la pantalla de inicio de sesión: paisajes de Chile que se alternan con un fundido.
 * - Computador: videos 1080p. Celular: videos 720p (más livianos).
 * - Solo con ahorro de datos o conexión muy lenta se muestran imágenes fijas con un acercamiento lento.
 * - Es decorativo y sin sonido, por eso se reproduce aunque el sistema tenga reducidas las animaciones.
 * - Solo se descarga el primer video al inicio; el siguiente se precarga poco antes de su turno.
 */
@Component({
  selector: 'app-fondo-login',
  templateUrl: './fondo-login.html',
  styleUrl: './fondo-login.scss',
})
export class FondoLogin {
  protected readonly escenas = ESCENAS;
  protected readonly actual = signal(0);
  /** true = videos; false = imágenes fijas (ahorro de datos o conexión lenta). */
  protected readonly conVideo = signal(false);
  /** Sufijo del archivo de video según el tamaño de pantalla ('' = 1080p, '-720' = 720p). */
  private sufijo = '';

  private readonly videos = viewChildren<ElementRef<HTMLVideoElement>>('video');

  protected imagen(escena: Escena): string {
    return `${CARPETA}${escena.nombre}.webp`;
  }

  constructor() {
    const destroyRef = inject(DestroyRef);

    afterNextRender(() => {
      const conexion = (navigator as Navigator & { connection?: { saveData?: boolean; effectiveType?: string } })
        .connection;
      const conexionLenta = !!conexion?.saveData || ['slow-2g', '2g'].includes(conexion?.effectiveType ?? '');
      this.sufijo = matchMedia('(max-width: 767px)').matches ? '-720' : '';
      this.conVideo.set(!conexionLenta);

      let precarga: ReturnType<typeof setTimeout> | undefined;
      let intervalo: ReturnType<typeof setInterval> | undefined;
      const programarPrecarga = () => {
        clearTimeout(precarga);
        precarga = setTimeout(
          () => this.cargar((this.actual() + 1) % this.escenas.length),
          DURACION_ESCENA_MS - ANTICIPO_PRECARGA_MS,
        );
      };
      const iniciarRotacion = () => {
        if (intervalo) return;
        programarPrecarga();
        intervalo = setInterval(() => {
          if (document.hidden) {
            return;
          }
          const siguiente = (this.actual() + 1) % this.escenas.length;
          this.actual.set(siguiente);
          this.reproducir(siguiente);
          programarPrecarga();
        }, DURACION_ESCENA_MS);
      };

      // Esperar a que se dibujen los <video>. La rotación empieza cuando el primer video
      // realmente se reproduce (así no se corta mientras carga), con un máximo de espera.
      setTimeout(() => {
        const primero = this.cargar(0);
        if (this.conVideo() && primero) {
          primero.addEventListener('playing', iniciarRotacion, { once: true });
          setTimeout(iniciarRotacion, 6000);
          this.reproducir(0);
        } else {
          iniciarRotacion();
        }
      });

      const alCambiarVisibilidad = () => {
        const video = this.videos()[this.actual()]?.nativeElement;
        if (!video) return;
        document.hidden ? video.pause() : video.play().catch(() => undefined);
      };
      document.addEventListener('visibilitychange', alCambiarVisibilidad);

      destroyRef.onDestroy(() => {
        clearInterval(intervalo);
        clearTimeout(precarga);
        intervalo = -1 as unknown as ReturnType<typeof setInterval>; // evita iniciar la rotación tras destruir
        document.removeEventListener('visibilitychange', alCambiarVisibilidad);
      });
    });
  }

  /** Asigna el archivo de video a una escena (solo la primera vez) para que el navegador lo descargue. */
  private cargar(indice: number): HTMLVideoElement | undefined {
    const video = this.videos()[indice]?.nativeElement;
    if (!video) return undefined;
    video.muted = true; // necesario para la reproducción automática en todos los navegadores
    if (!video.src) {
      video.src = `${CARPETA}${this.escenas[indice].nombre}${this.sufijo}.webm`;
      video.preload = 'auto';
    }
    return video;
  }

  /** Reproduce el video de la escena indicada desde el inicio y pausa los demás tras el fundido. */
  private reproducir(indice: number): void {
    if (!this.conVideo()) {
      return;
    }
    const video = this.cargar(indice);
    if (video) {
      video.currentTime = 0;
      video.play().catch(() => undefined);
    }
    this.videos().forEach((v, i) => {
      if (i !== indice) {
        setTimeout(() => v.nativeElement.pause(), 1500);
      }
    });
  }
}

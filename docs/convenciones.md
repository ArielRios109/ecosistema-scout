# Convenciones del proyecto

Reglas que todo el equipo (y cualquier herramienta de IA que se use) debe seguir en **cada** cambio.
Si una regla cambia, se actualiza este archivo en el mismo pull request.

## 1. Interfaz (frontend)

1. **Todo con Angular Material 3.** Componentes, formularios, diálogos, menús, tablas y temas usan Angular Material 3. No se agregan otras bibliotecas visuales (Bootstrap, Tailwind, librerías de toasts, etc.).
2. **Colores y tipografía desde el tema.** Usar los tokens del sistema (`var(--mat-sys-primary)`, `var(--mat-sys-body-medium)`, …) en vez de colores o fuentes fijas. El tema se define en `frontend/src/styles.scss`.
3. **Adaptable al celular.** Cada pantalla debe verse bien en computador y en celular (los apoderados usan el portal desde el teléfono).
4. **Estructura:** cada módulo vive en su carpeta `src/app/features/<modulo>/` y se carga bajo demanda (*lazy loading*). Lo transversal va en `core/` (servicios únicos) y `shared/` (componentes reutilizables).

## 2. Identidad visual

**Concepto del logo:** un pañolín scout verde con franjas doradas y un pasador de cuero; las puntas cruzadas con el pasador forman la **T** de Talí. No usa emblemas oficiales de la AGSCh.

**Colores de marca:**

| Color | Código | Uso |
|---|---|---|
| Verde | `#2E7D32` | Color principal del logo y del tema |
| Verde oscuro | `#16361a` | Contornos del logo |
| Dorado | `#F2B33D` | Franjas del pañolín |
| Café pasador | `#8D5524` | Pasador de cuero |

**Versiones del logo** (en `frontend/public/img/`):

| Archivo | Dónde se usa |
|---|---|
| `tali-logo.svg` | Sobre fondos claros: pantalla de inicio de sesión, documentos a color |
| `tali-logo-blanco.svg` | Sobre fondos verdes u oscuros: barra superior de la aplicación |
| `tali-icono.svg` (+ `favicon-32/192/512.png`) | Ícono de la pestaña del navegador y del celular |
| `tali-logo-una-tinta.svg` | Documentos impresos en blanco y negro, sellos |

En pantalla el nombre siempre se escribe **Talí** (con tilde); en código y URLs, `tali`.

## 3. Manejo de errores y mensajes

1. **Todos los errores y avisos se muestran con un snackbar de Angular Material 3, centrado arriba.**
2. Siempre a través del servicio común `Notificaciones` (`src/app/core/notificaciones/notificaciones.ts`), con sus métodos `error()`, `aviso()` y `exito()`. No llamar a `MatSnackBar` directamente desde los componentes.
3. Los errores HTTP los muestra **automáticamente** el interceptor (`core/auth/auth-interceptor.ts`). Un componente no debe volver a mostrarlos. Si una petición necesita manejar su error de otra forma, usa el token `SIN_NOTIFICACION_ERROR`.
4. **No** se usa texto rojo suelto en la página, `alert()` ni `console.log` como forma de avisar al usuario.
5. **Excepción:** los mensajes de validación bajo cada campo de un formulario (`<mat-error>`, por ejemplo "Ingresa tu correo") se mantienen, porque guían el llenado y no son errores del sistema.
6. En la API, los errores se responden como *ProblemDetails* con un `title` claro en español, para que el snackbar lo muestre tal cual.

## 4. Seguridad y permisos

1. Los permisos por rol se aplican **siempre en la API** (políticas en `Modulos/Base/Autorizacion/Politicas.cs`). El frontend solo oculta menús y rutas (`core/auth/permisos.ts`); ambas tablas deben mantenerse iguales.
2. **Nunca** se suben secretos reales al repositorio: las contraseñas, claves JWT y cadenas de conexión de producción van en `.env` (Docker) o en variables de entorno.
3. El repositorio solo trae **valores de desarrollo** públicos (en `docker-compose.yml` y `appsettings.Development.json`) para clonar y ejecutar sin configurar nada. Todos contienen la palabra "desarrollo", y la API se niega a arrancar en producción si los detecta (`Configuracion/ValidacionProduccion.cs`).

## 5. Backend (.NET)

1. Código y nombres en español, organizados por módulo en `Modulos/<Modulo>/`.
2. Cambios a la base de datos solo mediante migraciones de Entity Framework Core (`Datos/Migraciones`); nunca modificar la base a mano.
3. Cada funcionalidad nueva incluye sus pruebas (xUnit) en `backend/tests`.

## 6. Git y pull requests

1. **Ramas:** una por módulo o funcionalidad, con nombre en minúsculas (`base`, `miembros`, `inventario`, `finanzas`, `portal`). Nunca se trabaja directo en `main`.
2. **Pull request a `main`** revisado y aprobado por el otro integrante antes del merge.
3. **Commits con Conventional Commits en español**, en tiempo presente: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`. Ejemplo: `feat: agrega registro de préstamos`.
4. Cada commit es un cambio coherente y completo; no se hacen commits de prueba ("wip", "prueba", "arreglo").
5. Antes de abrir un pull request: compilar, ejecutar las pruebas (`dotnet test backend` y `npx ng test` en `frontend/`) y tomar las capturas de evidencia en `docs/evidencias/<rama>/`.

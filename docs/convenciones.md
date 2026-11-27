# Convenciones del proyecto

Reglas que todo el equipo (y cualquier herramienta de IA que se use) debe seguir en **cada** cambio.
Si una regla cambia, se actualiza este archivo en el mismo pull request.

## 1. Interfaz (frontend)

1. **Todo con Angular Material 3.** Componentes, formularios, diálogos, menús, tablas y temas usan Angular Material 3. No se agregan otras bibliotecas visuales (Bootstrap, Tailwind, librerías de toasts, etc.).
2. **Sin colores hexadecimales ni colores fijos.** En componentes, plantillas y estilos solo se usan los tokens del sistema de Material 3 (`var(--mat-sys-primary)`, `var(--mat-sys-surface)`, `var(--mat-sys-scrim)`, …); para transparencias, `color-mix(in srgb, var(--mat-sys-…) 40%, transparent)`. La tipografía también sale de los tokens (`var(--mat-sys-body-medium)`, …).
   - El tema se genera desde los colores de marca con `ng generate @angular/material:theme-color` (`frontend/src/estilos/_tema-colores.scss`) y se aplica en `frontend/src/styles.scss`.
   - **Excepciones:** el archivo generado del tema y los SVG del logo (son imágenes) llevan sus colores dentro.
3. **Utilidades CSS reducidas.** Para disposición, separación, márgenes y texto se usan las clases cortas de `frontend/src/estilos/_utilidades.scss` (`flex`, `flex-col`, `gap-2`, `mt-3`, `texto-secundario`, …). El SCSS de cada componente queda solo para lo propio de ese componente. La lista se mantiene corta (≈30 clases); no se agregan Tailwind ni Bootstrap.
4. **Spinner global.** Hay un solo indicador de carga para toda la app (`core/carga/`): lo muestra automáticamente el interceptor HTTP cuando una petición a la API tarda más de 300 ms. Los componentes **no** agregan spinners ni barras de progreso propios; para una petición en segundo plano se usa el token `SIN_SPINNER`.
5. **Adaptable al celular.** Cada pantalla debe verse bien en computador y en celular (los apoderados usan el portal desde el teléfono).
6. **Estructura:** cada módulo vive en su carpeta `src/app/features/<modulo>/` y se carga bajo demanda (*lazy loading*). Lo transversal va en `core/` (servicios únicos) y `shared/` (componentes reutilizables).

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
| `tali-logo.svg` | Sobre fondos claros: pantalla de inicio de sesión, barra superior (clara), documentos a color |
| `tali-logo-blanco.svg` | Sobre fondos verdes u oscuros |
| `tali-icono.svg` (+ `favicon-32/192/512.png`) | Ícono de la pestaña del navegador y del celular |
| `tali-logo-una-tinta.svg` | Documentos impresos en blanco y negro, sellos |

En pantalla el nombre siempre se escribe **Talí** (con tilde); en código y URLs, `tali`.

## 3. Manejo de errores y mensajes

1. **Todos los errores y avisos se muestran con un snackbar de Angular Material 3, centrado arriba.**
2. Siempre a través del servicio común `Notificaciones` (`src/app/core/notificaciones/notificaciones.ts`), con sus métodos `error()`, `aviso()` y `exito()`. No llamar a `MatSnackBar` directamente desde los componentes.
3. Los errores HTTP los muestra **automáticamente** el interceptor (`core/auth/auth-interceptor.ts`). Un componente no debe volver a mostrarlos. Si una petición necesita manejar su error de otra forma, usa el token `SIN_NOTIFICACION_ERROR`.
4. **No** se usa texto rojo suelto en la página, `alert()` ni `console.log` como forma de avisar al usuario.
5. **Excepción:** los mensajes de validación bajo cada campo de un formulario (`<mat-error>`, por ejemplo "Ingresa tu correo") se mantienen, porque guían el llenado y no son errores del sistema.
6. La API responde siempre con `ResponseData` (ver Backend). El interceptor `core/api/respuesta-interceptor.ts` entrega a cada pantalla directamente los `datos`; en los errores, el snackbar muestra el `mensaje` (y los `errores` de validación) que envía la API. Para mostrar también un mensaje de éxito de la API se usa el token `MOSTRAR_MENSAJE_EXITO`.

## 4. Seguridad y permisos

1. Los permisos por rol se aplican **siempre en la API** (políticas en `Modulos/Base/Autorizacion/Politicas.cs`). El frontend solo oculta menús y rutas (`core/auth/permisos.ts`); ambas tablas deben mantenerse iguales.
2. **Nunca** se suben secretos reales al repositorio: las contraseñas, claves JWT y cadenas de conexión de producción van en `.env` (Docker) o en variables de entorno.
3. El repositorio solo trae **valores de desarrollo** públicos (en `docker-compose.yml` y `appsettings.Development.json`) para clonar y ejecutar sin configurar nada. Todos contienen la palabra "desarrollo", y la API se niega a arrancar en producción si los detecta (`Configuracion/ValidacionProduccion.cs`).

## 5. Backend (.NET)

1. Código y nombres en español, organizados por módulo en `Modulos/<Modulo>/`.
2. Cambios a la base de datos solo mediante migraciones de Entity Framework Core (`Datos/Migraciones`); nunca modificar la base a mano.
3. Cada funcionalidad nueva incluye sus pruebas (xUnit) en `backend/tests`.
4. **Todas las respuestas pasan por `ResponseData<T>`** (`Comun/ResponseData.cs`): `{ exito, mensaje, datos, errores }`, usando `ResponseData.Ok(...)`, `ResponseData.Error(...)` o `ResponseData.ErrorConDatos(...)`.
   - Se mantiene siempre el **código HTTP correcto** (200, 400, 401, 403, 404, 500…); nunca "200 con exito = false".
   - Los mensajes van en español y pensados para el usuario final.
   - El manejo global (`Comun/ManejoErrores.cs`) convierte excepciones (500), errores de validación (400 con la lista de `errores`) y respuestas sin cuerpo (401, 403, 404) al mismo formato. Las excepciones no exponen detalles internos al usuario; se registran en el log.

## 6. Git y pull requests

1. **Ramas:** una por módulo o funcionalidad, con nombre en minúsculas (`base`, `miembros`, `inventario`, `finanzas`, `portal`). Nunca se trabaja directo en `main`.
2. **Pull request a `main`** revisado y aprobado por el otro integrante antes del merge.
3. **Commits con Conventional Commits en español**, en tiempo presente: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`, `test:`. Ejemplo: `feat: agrega registro de préstamos`.
4. Cada commit es un cambio coherente y completo; no se hacen commits de prueba ("wip", "prueba", "arreglo").
5. Antes de abrir un pull request: compilar, ejecutar las pruebas (`dotnet test backend` y `npx ng test` en `frontend/`) y tomar las capturas de evidencia en `docs/evidencias/<rama>/`.

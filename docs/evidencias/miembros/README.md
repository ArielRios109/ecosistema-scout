# Evidencias · etapa `miembros`

Capturas tomadas el 27-11-2026 con el sistema corriendo en Docker (`docker compose up`), sobre una base de datos nueva con los datos de demostración, que son ficticios.

| Archivo | Qué muestra |
|---|---|
| `01-unidades-y-patrullas.png` | Unidades por rama con sus patrullas y la cantidad de beneficiarios activos |
| `02-nueva-unidad.png` | Formulario de unidad en un diálogo (solo administración) |
| `03-beneficiarios-listado.png` | Listado con búsqueda, filtros por unidad y estado, y paginación |
| `04-beneficiarios-busqueda.png` | Búsqueda por nombre ("díaz") |
| `06-ficha-datos.png` | Ficha del beneficiario: pestaña Datos |
| `07-ficha-apoderados.png` | Apoderados vinculados, con parentesco, principal y cuenta del Portal |
| `08-ficha-contactos.png` | Contactos de emergencia |
| `09-ficha-medica.png` | Ficha médica (en la base de datos se guarda cifrada) |
| `10-ficha-medica-edicion.png` | Edición de la ficha médica |
| `11-historial-cambio-de-unidad.png` | Historial automático: ingreso y paso de la Manada a la Tropa |
| `12-reingreso.png` | Reingreso de un beneficiario retirado, eligiendo unidad y patrulla |
| `13-apoderados.png` | Listado de apoderados con su estado de cuenta del Portal |
| `14-voluntarios.png` | Voluntarios con sus cargos vigentes |
| `15-cargos-del-voluntario.png` | Cargos del voluntario: vigentes, historial y asignación |
| `16-dirigente-unidades.png` | El dirigente ve las unidades y agrega patrullas, pero no crea unidades |
| `17-celular-beneficiarios.png` | Listado en celular, en tarjetas |
| `18-celular-ficha.png` | Ficha del beneficiario en celular |
| `19-pruebas-backend.png` | 98 pruebas xUnit correctas |
| `20-pruebas-frontend.png` | 32 pruebas Vitest correctas |
| `21-usuarios-listado.png` | Administración de usuarios: búsqueda, filtros y acciones |
| `22-usuario-nuevo.png` | Creación de un usuario con contraseña temporal |

## Refactor antes del PR

| Archivo | Qué muestra |
|---|---|
| `23-inscripcion-rut-ya-inscrito.png` | Inscripción, paso 1: el RUT ya existe y se ofrece ir a su ficha |
| `24-inscripcion-apoderados-y-contactos.png` | Paso 4: apoderados y contactos agregados antes de guardar |
| `25-inscripcion-resumen.png` | Paso 6: resumen antes de inscribir |
| `26-inscripcion-guardada.png` | Beneficiario inscrito (todo se guarda en una sola operación) |
| `27-confirmar-eliminar.png` | Modal de confirmación global, variante "eliminar" |
| `28-descartar-cambios.png` | Al salir con el formulario a medio llenar: "¿Descartar los cambios?" |
| `29-celular-inscripcion.png` | Inscripción en celular (stepper vertical) |
| `30-celular-apoderado-rut-registrado.png` | Apoderado en celular: RUT ya registrado y el resto de los campos bloqueados |

## Reglas del PGS

| Archivo | Qué muestra |
|---|---|
| `31-pgs-cupos-por-unidad-y-subgrupo.png` | Cupos por unidad (x/32) y subgrupo según la rama (seisenas x/6, patrullas x/8, equipos, comunidades) |
| `32-pgs-avisos-en-listado.png` | Avisos de egreso y de ficha médica desactualizada en el listado |
| `33-pgs-ficha-medica-desactualizada.png` | Franja de ficha médica con más de 3 meses sin cambios |
| `34-pgs-aviso-de-egreso.png` | Aviso de egreso de un beneficiario de 20 años |

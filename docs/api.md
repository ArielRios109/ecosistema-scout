# API de Talí

API REST de .NET 10 que consume el frontend Angular. Todas las rutas parten con `/api` y responden JSON con el formato único `ResponseData`.

## Recorrido de una petición

```mermaid
sequenceDiagram
    autonumber
    actor U as Persona
    participant A as Angular (componente + servicio)
    participant I as Interceptores (spinner → auth → respuesta)
    participant N as nginx
    participant C as Controlador .NET
    participant E as EF Core + bitácora
    participant P as PostgreSQL

    U->>A: Acción en pantalla (p. ej. "Registrar beneficiario")
    A->>I: HttpClient.post('/api/miembros/beneficiarios', datos)
    I->>I: Muestra el spinner global y agrega "Authorization: Bearer <JWT>"
    I->>N: Petición HTTP
    N->>C: Reenvía /api/* al contenedor de la API
    C->>C: Valida el token, la política del rol y los datos ([Required], [RutValido]...)
    C->>E: Reglas de negocio y SaveChanges
    E->>P: INSERT / UPDATE (la bitácora registra antes y después)
    P-->>E: OK
    E-->>C: Entidad guardada
    C-->>N: 201 + ResponseData { exito, mensaje, datos }
    N-->>I: Respuesta
    I->>I: Oculta el spinner y entrega solo "datos" al servicio
    I-->>U: Snackbar centrado arriba con el mensaje (éxito o error)
    I-->>A: datos
```

Si algo falla, la API responde con el código HTTP que corresponde y un `ResponseData` con `exito: false`. El interceptor de respuesta muestra `mensaje` (y `errores`, si vienen) en el snackbar. Así ningún componente necesita manejar errores por su cuenta.

## Convención de rutas

```
/api/{modulo}/{recurso}[/{id}][/{subrecurso | accion}]
```

| Regla | Ejemplo |
|---|---|
| Módulo y recurso en español, en plural y en kebab-case | `/api/miembros/beneficiarios`, `/api/miembros/beneficiarios/7/ficha-medica` |
| El id va en la ruta, nunca en el cuerpo | `PUT /api/miembros/apoderados/3` |
| Lo que depende de otro recurso va anidado bajo ese recurso | `/api/miembros/beneficiarios/7/contactos` |
| Acción que no es un CRUD: `POST` con un verbo en infinitivo | `POST /api/miembros/beneficiarios/7/retirar` |
| Filtros, búsqueda y paginación como parámetros de consulta | `?buscar=pérez&unidadId=2&estado=activo&pagina=1&tamano=20` |

Módulos: `auth`, `base`, `miembros`, `inventario`, `finanzas` y `portal`. La única excepción es `/api/health`, que usa la ruta de salud habitual y la consultan Docker y los monitores.

### Verbos

| Verbo | Uso | Respuesta habitual |
|---|---|---|
| `GET` | Consultar (nunca modifica) | 200 |
| `POST` | Crear, o ejecutar una acción | 201 al crear (con encabezado `Location`), 200 en una acción |
| `PUT` | Reemplazar los datos editables de un recurso | 200 |
| `DELETE` | Eliminar (solo lo que no tiene historial; lo demás se **desactiva** o se **retira**) | 200 |

## Formato de respuesta: `ResponseData<T>`

```json
{ "exito": true, "mensaje": "Beneficiario registrado.", "datos": { "id": 21, "rut": "12.345.678-5" }, "errores": null }
```

| Código | Cuándo | Ejemplo de `mensaje` |
|---|---|---|
| 200 / 201 | Todo bien | `"Apoderado vinculado."` (o `null` en consultas) |
| 400 | Datos inválidos; `errores` trae el detalle por campo | `"Revisa los datos ingresados."`, `errores: ["El RUT no es válido."]` |
| 401 | Sin sesión o con el token vencido (el frontend vuelve al inicio de sesión) | `"Debes iniciar sesión para continuar."` |
| 403 | El rol no tiene permiso, o falta cambiar la contraseña temporal | `"No tienes permiso para realizar esta acción."` |
| 404 | El recurso no existe | `"No se encontró el beneficiario."` |
| 409 | Choca con una regla de negocio | `"Ya existe un beneficiario con ese RUT."`, `"La unidad tiene beneficiarios activos."` |
| 500 | Error inesperado (el detalle solo queda en el log del servidor) | `"Ocurrió un error en el servidor. Intenta nuevamente en unos minutos."` |

### Límites de los campos

Cada texto tiene el mismo largo máximo en el formulario (con contador visible), en la API (`[MaxLength]` con mensaje en español) y en la base de datos. Si se envía un JSON mal formado, la API responde 400 con un mensaje general, sin detalles internos.

### Listados paginados

Los listados extensos reciben `pagina` (desde 1), `tamano` (entre 1 y 100; por defecto 20) y `buscar`, y devuelven:

```json
{ "elementos": [ ... ], "total": 57, "paginaActual": 1, "tamano": 20, "totalPaginas": 3 }
```

El RUT se acepta con o sin puntos y guion (máximo 12 caracteres; se rechazan letras distintas de K y otros símbolos), se guarda normalizado (`12345678-5`) y se entrega formateado (`12.345.678-5`). Las fechas sin hora van como `aaaa-mm-dd` y las fechas con hora en UTC (ISO 8601).

## Autenticación y permisos

1. `POST /api/auth/login` entrega un **JWT** válido por 8 horas con el id, el nombre y los roles de la persona.
2. El frontend lo guarda y lo envía en cada petición: `Authorization: Bearer <token>`.
3. Cada controlador exige una **política** según el módulo, y algunas acciones exigen una política más estricta.
4. Una cuenta con contraseña temporal solo puede usar `auth/me` y `auth/cambiar-password` hasta que la cambie (403 en todo lo demás).

| Política | Roles | Se usa en |
|---|---|---|
| `Miembros` | administrador, dirigente | Todo `/api/miembros` (por ahora el dirigente ve todas las unidades) |
| `Administracion` | administrador | `/api/base/*`; crear, editar, desactivar o eliminar unidades; cuenta del Portal |
| `Inventario` / `InventarioEscritura` / `InventarioGestion` | administrador, tesorería, dirigente, revisor / sin el revisor / solo administrador y tesorería | `/api/inventario` (consulta / préstamos / bienes, categorías y bajas) |
| `Finanzas` / `FinanzasEscritura` | administrador, tesorería, revisor / administrador y tesorería | `/api/finanzas` |
| `Portal` | administrador, tesorería, apoderado | `/api/portal` |

El frontend oculta los menús según el rol (`core/auth/permisos.ts`), pero la seguridad real está en la API.

## Endpoints

### Autenticación y salud

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | Público | Inicia sesión y entrega el JWT |
| GET | `/api/auth/me` | Con sesión | Datos de la persona conectada |
| POST | `/api/auth/cambiar-password` | Con sesión | Cambia la contraseña (también la temporal) y entrega un token nuevo |
| GET | `/api/health` | Público | Estado de la API y de la conexión a la base de datos |

### Base: usuarios y roles

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| GET | `/api/base/usuarios` | administrador | Listado paginado (`buscar`, `rol`, `activo`) |
| GET | `/api/base/usuarios/{id}` | administrador | Detalle |
| POST | `/api/base/usuarios` | administrador | Crea la cuenta con contraseña temporal (se muestra una sola vez) |
| PUT | `/api/base/usuarios/{id}` | administrador | Edita el nombre y el correo |
| PUT | `/api/base/usuarios/{id}/roles` | administrador | Asigna roles (no puedes quitarte tu propio rol de administrador) |
| POST | `/api/base/usuarios/{id}/activar` · `/desactivar` | administrador | Habilita o bloquea el acceso (no puedes desactivarte) |
| POST | `/api/base/usuarios/{id}/restablecer-password` | administrador | Genera una nueva contraseña temporal |
| GET | `/api/base/roles` | administrador | Roles disponibles |

### Base: inicio

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| GET | `/api/base/inicio` | cualquier usuario | Resumen ya filtrado por rol, calculado con agregados en la base de datos |

Cada sección del resumen viene solo si el rol puede verla (si no, llega en `null`):

| Sección | Quién la recibe | Contenido |
|---|---|---|
| `miembros` | admin, dirigente, revisor | Beneficiarios activos por rama, voluntarios con cargo vigente, cupo por unidad y conteo de avisos (fichas con más de 3 meses, sin ficha, egresos, sobre la edad máxima). El dirigente ve solo su unidad (la del cargo vigente de su voluntario vinculado) y `alcance` trae su nombre |
| `inventario` | admin, tesorería, dirigente, revisor | Bienes, préstamos activos y vencidos, y los 5 vencidos más antiguos |
| `usuarios` | admin | Usuarios activos y cuántos tienen la contraseña temporal pendiente |
| `misBeneficiarios` | apoderado | Sus beneficiarios activos con unidad, parentesco y avisos |
| `finanzasProximamente` | admin, tesorería, revisor | Indica que se muestre la tarjeta de Finanzas mientras el módulo no esté listo |

### Reglas del PGS que valida la API

| Regla | Respuesta |
|---|---|
| Máximo 32 beneficiarios activos por unidad | 409 al inscribir, cambiar de unidad o reingresar si la unidad está completa |
| Subgrupo según la rama: seisena (máx. 6) en golondrinas y lobatos, patrulla (máx. 8) en guías y scouts, comunidad en pioneros y equipo en caminantes (sin máximo) | 409 si el subgrupo está completo; `UnidadDto.subgrupo` informa nombre y máximo |
| Desde los 21 años no se puede participar | 400 al inscribir, editar (si está activo) o reingresar |
| Aviso de egreso desde los 18 y ficha médica con más de 3 meses sin cambios | Campos `avisoEdad` y `estadoFicha` en beneficiarios, y `desactualizada` en la ficha |

### Miembros: unidades y patrullas

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| GET | `/api/miembros/unidades` | admin, dirigente | Unidades con sus patrullas y beneficiarios activos (`incluirInactivas`) |
| GET | `/api/miembros/unidades/{id}` | admin, dirigente | Detalle |
| POST / PUT | `/api/miembros/unidades[/{id}]` | administrador | Crea o edita (nombre y rama) |
| POST | `/api/miembros/unidades/{id}/desactivar` · `/activar` | administrador | 409 si se intenta desactivar con beneficiarios activos |
| DELETE | `/api/miembros/unidades/{id}` | administrador | Solo si no tiene beneficiarios, patrullas, historial ni cargos |
| POST | `/api/miembros/unidades/{unidadId}/patrullas` | admin, dirigente | Crea una patrulla |
| PUT | `/api/miembros/patrullas/{id}` | admin, dirigente | Renombra |
| POST | `/api/miembros/patrullas/{id}/desactivar` · `/activar` | admin, dirigente | Cambia el estado |
| DELETE | `/api/miembros/patrullas/{id}` | admin, dirigente | Solo si no tiene beneficiarios |

### Miembros: beneficiarios

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| GET | `/api/miembros/beneficiarios` | admin, dirigente | Listado paginado (`buscar` por nombre o RUT, `unidadId`, `patrullaId`, `estado`, `aviso`: `egreso`, `edad-maxima`, `ficha-desactualizada` o `sin-ficha`; 400 si no es válido) |
| GET | `/api/miembros/beneficiarios/{id}` | admin, dirigente | Ficha con edad, unidad y rama |
| GET | `/api/miembros/beneficiarios/existe?rut=` | admin, dirigente | Indica si el RUT ya está inscrito (`{ existe, id, nombre }`); lo usa el formulario antes de pedir el resto |
| POST | `/api/miembros/beneficiarios/inscripcion` | admin, dirigente | **Inscripción completa** en una sola transacción: beneficiario, apoderados (existentes o nuevos), contactos y ficha médica |
| POST | `/api/miembros/beneficiarios` | admin, dirigente | Registra solo los datos del beneficiario y anota el **ingreso** en el historial |
| PUT | `/api/miembros/beneficiarios/{id}` | admin, dirigente | Edita; el cambio de unidad o de patrulla queda en el historial |
| POST | `/api/miembros/beneficiarios/{id}/retirar` | admin, dirigente | Retira (fecha y motivo); los datos se conservan |
| POST | `/api/miembros/beneficiarios/{id}/reingresar` | admin, dirigente | Reingresa, en la misma unidad u otra |
| GET | `/api/miembros/beneficiarios/{id}/historial` | admin, dirigente | Movimientos, del más reciente al más antiguo, con quién los registró |
| GET / POST | `/api/miembros/beneficiarios/{id}/contactos` | admin, dirigente | Contactos de emergencia (máximo 5) |
| PUT / DELETE | `/api/miembros/beneficiarios/{id}/contactos/{contactoId}` | admin, dirigente | Edita o elimina un contacto |
| GET / PUT | `/api/miembros/beneficiarios/{id}/ficha-medica` | admin, dirigente | Ficha médica (se guarda **cifrada**; registra quién la modificó) |
| GET / POST | `/api/miembros/beneficiarios/{id}/apoderados` | admin, dirigente | Vínculos con apoderados; el primero queda como principal |
| PUT / DELETE | `/api/miembros/beneficiarios/{id}/apoderados/{apoderadoId}` | admin, dirigente | Cambia parentesco o principal, o desvincula |

### Miembros: apoderados

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| GET | `/api/miembros/apoderados` | admin, dirigente | Listado paginado (`buscar` por nombre, correo o RUT) |
| GET | `/api/miembros/apoderados/existe?rut=` | admin, dirigente | Indica si el RUT ya está registrado (para ofrecer usarlo en vez de duplicarlo) |
| GET | `/api/miembros/apoderados/{id}` | admin, dirigente | Detalle con sus beneficiarios |
| POST / PUT | `/api/miembros/apoderados[/{id}]` | admin, dirigente | Registra o edita |
| DELETE | `/api/miembros/apoderados/{id}` | admin, dirigente | Solo sin beneficiarios vinculados |
| POST | `/api/miembros/apoderados/{id}/cuenta` | administrador | Crea su cuenta del Portal con contraseña temporal, o vincula la existente |

### Miembros: voluntarios y cargos

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| GET | `/api/miembros/voluntarios` | admin, dirigente | Listado paginado con cargos vigentes (`buscar`, `unidadId`, `conCargo`) |
| GET | `/api/miembros/voluntarios/existe?rut=` | admin, dirigente | Indica si el RUT ya está registrado |
| GET | `/api/miembros/voluntarios/{id}` | admin, dirigente | Detalle con todos sus cargos (vigentes e historial) |
| POST / PUT | `/api/miembros/voluntarios[/{id}]` | admin, dirigente | Registra o edita |
| DELETE | `/api/miembros/voluntarios/{id}` | admin, dirigente | Solo si nunca tuvo cargos |
| POST | `/api/miembros/voluntarios/{id}/cargos` | admin, dirigente | Asigna un cargo (de grupo o de una unidad) |
| PUT / DELETE | `/api/miembros/voluntarios/{id}/cargos/{cargoId}` | admin, dirigente | Corrige o elimina un registro erróneo |
| POST | `/api/miembros/voluntarios/{id}/cargos/{cargoId}/terminar` | admin, dirigente | Termina el cargo (pasa al historial) |

### Inventario

Lo ven administración, tesorería, dirigentes y la comisión revisora (`Inventario`). Bienes, categorías y bajas los administran administración y tesorería (`InventarioGestion`); los préstamos también los registra el dirigente (`InventarioEscritura`).

| Método | Ruta | Roles | Descripción |
|---|---|---|---|
| GET | `/api/inventario/categorias` | Inventario | Categorías con su cantidad de bienes |
| POST / PUT / DELETE | `/api/inventario/categorias[/{id}]` | admin, tesorería | Crear, renombrar o eliminar (solo sin bienes) |
| GET | `/api/inventario/bienes` | Inventario | Listado paginado (`buscar`, `categoriaId`, `estado`, `ubicacion`, `incluirBajas`) con disponible y préstamos vencidos |
| GET | `/api/inventario/bienes/{id}` | Inventario | Ficha con préstamos y baja |
| POST / PUT | `/api/inventario/bienes[/{id}]` | admin, tesorería | Registra (código correlativo `INV-00001`) o edita; la cantidad no puede quedar bajo lo prestado |
| POST | `/api/inventario/bienes/{id}/baja` | admin, tesorería | Baja con motivo; no se permite con préstamos activos y el bien queda en el historial |
| PUT / GET / DELETE | `/api/inventario/bienes/{id}/foto` | admin, tesorería (GET: Inventario) | Foto JPG, PNG o WebP de hasta 3 MB; se valida el tipo real del archivo |
| GET | `/api/inventario/bienes/exportar` | Inventario | Planilla Excel con los filtros del listado |
| GET | `/api/inventario/responsables` | Inventario | Voluntarios que pueden ser responsables (solo id y nombre) |
| GET | `/api/inventario/prestamos` | Inventario | Préstamos paginados (`estado`: activos, vencidos o devueltos) |
| POST | `/api/inventario/bienes/{id}/prestamos` | admin, tesorería, dirigente | Préstamo; no se presta más de lo disponible |
| POST | `/api/inventario/prestamos/{id}/devolucion` | admin, tesorería, dirigente | Devolución con estado; "perdido" descuenta la cantidad del bien |

Las fotos se guardan fuera de la base de datos, en la carpeta `Archivos:Ruta` (en Docker, el volumen `archivos`), detrás de la interfaz `IAlmacenArchivos`. Así se puede pasar a un almacenamiento en la nube sin tocar los controladores.

### Módulos en construcción

`GET /api/finanzas` y `/api/portal` solo informan que el módulo está en construcción, pero ya están protegidos por su política.

## Probar la API

Con el entorno levantado (`docker compose up -d`), la API responde en `http://localhost:8081`. En desarrollo, la especificación OpenAPI está en `/openapi/v1.json`:

```bash
curl -s -X POST http://localhost:8081/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@demo.cl","password":"TaliDemo2026"}'
```

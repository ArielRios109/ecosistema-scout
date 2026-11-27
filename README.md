# Ecosistema Digital Scout

**Talí** es el nombre de la aplicación del proyecto Ecosistema Digital Scout. En el movimiento scout, un *talí* (o *thalí* / *tally*) es el cuaderno o bitácora de la patrulla o unidad donde se registran la historia, las actividades, los campamentos, las tradiciones y las experiencias de sus integrantes. La aplicación cumple ese mismo rol en forma digital para la gestión del grupo, por eso lleva su nombre.

Sistema web de gestión de miembros, inventario y financiamiento para grupos de la Asociación de Guías y Scouts de Chile (AGSCh), con un portal donde los apoderados revisan sus cuotas e informan pagos por transferencia.

## Qué problema resuelve

En los grupos scout la información administrativa vive en planillas, papeles y grupos de WhatsApp. Los pagos llegan como fotos de comprobantes, la tesorería los cruza a mano con la cartola y los vuelve a escribir en su libro; el equipamiento no tiene registro de préstamos y las fichas de salud de los menores quedan en celulares personales. Este sistema registra cada dato una sola vez y genera desde ahí el estado de cuenta de cada familia, el libro de control mensual (DAF-FOR-02) y el inventario (DAF-FOR-04).

**Dirigido a:** tesorería y Comité de apoderados, dirigentes, administradores de grupo y apoderados.

## Módulos del MVP

| Módulo | Estado |
|---|---|
| Base | Listo: inicio de sesión, usuarios y roles, contraseña temporal, bitácora |
| Gestión de Miembros | Listo |
| Inventario, Financiamiento y Portal de pagos | En construcción |

- **Base**: Inicio de sesión, usuarios, roles acumulables con alcance por unidad, configuración del grupo y bitácora de auditoría.
- **Gestión de Miembros**: Unidades y patrullas, beneficiarios, apoderados y sus vínculos, contactos de emergencia, ficha médica cifrada, voluntarios y cargos, historial de cada beneficiario.
- **Inventario**: Bienes con los campos del formulario DAF-FOR-04, préstamos y devoluciones, bajas con motivo y exportación a Excel.
- **Financiamiento**: Plan de cuentas, ingresos y egresos con comprobante correlativo, centros de costo, cuotas y cargos por beneficiario, estado de cuenta y libro de control mensual.
- **Portal de pagos**: Vista del apoderado con sus cargos, datos bancarios del grupo, informe de transferencias con comprobante, revisión por tesorería y recibo en PDF. Solo transferencias; sin pago en línea.

Fuera del alcance: pago en línea, eventos y campamentos, gestión documental y comunicaciones.

## Tecnologías

| Capa | Tecnología |
|---|---|
| Backend | C# con .NET 10 (API web con controladores), Entity Framework Core 10 + Npgsql |
| Autenticación | ASP.NET Identity y tokens JWT; roles administrador, tesoreria, dirigente, apoderado y revisor |
| Frontend | Angular 22 con Angular Material 3 (interfaz adaptable al celular) |
| Base de datos | PostgreSQL 18 |
| Infraestructura | Docker y Docker Compose (db, api, frontend con nginx) |
| Pruebas | xUnit (backend) y Vitest (frontend) |
| Editores | Visual Studio 2026 (backend) y VS Code (frontend) |

## Estructura del repositorio

```
backend/                     Solución .NET (abrir EcosistemaScout.slnx en Visual Studio)
  src/EcosistemaScout.Api/   API: Modulos/{Base,Miembros,Inventario,Finanzas,Portal}, Datos/ (DbContext y migraciones)
  tests/EcosistemaScout.Tests/  Pruebas de integración con xUnit
frontend/                    Aplicación Angular (abrir la carpeta en VS Code)
  src/app/core/              Autenticación (servicio, interceptor y guards)
  src/app/layout/shell/      Barra superior y menú lateral
  src/app/features/          Un módulo por carpeta, cargado bajo demanda
database/schema.sql          Modelo de datos de referencia (28 tablas)
docs/                        Convenciones, API, modelo de datos y evidencias
docker-compose.yml           Entorno completo con Docker
```

## Inicio rápido

Requisitos: [Git](https://git-scm.com) y [Docker Desktop](https://www.docker.com/products/docker-desktop/). No hace falta configurar nada.

```bash
git clone https://github.com/ArielRios109/ecosistema-scout.git
cd ecosistema-scout
docker compose up -d --build
```

Abre **http://localhost:8080** e ingresa con cualquiera de los usuarios de demostración, todos con la contraseña `TaliDemo2026`:

| Usuario | Rol |
|---|---|
| admin@demo.cl | Administración |
| tesoreria@demo.cl | Tesorería |
| dirigente@demo.cl | Dirigente |
| apoderado@demo.cl | Apoderado |
| revisor@demo.cl | Comisión revisora |

Al iniciar, la API crea la base de datos con sus migraciones y carga los datos iniciales (roles, plan de cuentas y centro de costo). En desarrollo también carga **datos de demostración ficticios** de Miembros: 6 unidades con sus patrullas, 20 beneficiarios, apoderados, contactos, fichas médicas y voluntarios. Los valores que se usan sin un archivo `.env` son **públicos y solo para desarrollo**.

## Desarrollo

Además de Docker: SDK de .NET 10, Node.js 22 y Angular CLI 22.

1. Levanta solo la base de datos: `docker compose up -d db` (queda en `localhost:5433`).
2. API: abre `backend/EcosistemaScout.slnx` en Visual Studio y presiona ▶ (perfil `http`, puerto 5095), o ejecuta `dotnet run --project backend/src/EcosistemaScout.Api`. Ya viene configurada para conectarse a la base de Docker (`appsettings.Development.json`).
3. Frontend: `cd frontend`, `npm install` y `npm start` → http://localhost:4200 (las llamadas a `/api` se reenvían a la API).

Si necesitas valores distintos en tu PC, usa `dotnet user-secrets` (API) o un archivo `.env` (Docker); ninguno de los dos se sube al repositorio.

## Producción

1. Copia `.env.example` como `.env` y reemplaza **todos** los valores `cambiar_esto` por valores propios y seguros (contraseña de la base de datos y una clave JWT aleatoria de al menos 32 caracteres).
2. Mantén `ASPNETCORE_ENVIRONMENT=Production` y `SEED_USUARIOS_DEMO=false`.
3. Levanta el sistema con `docker compose up -d --build` detrás de un proxy con HTTPS.

Por seguridad, la API **no arranca en producción** si detecta la clave o la contraseña de desarrollo, o los usuarios de demostración activados.

### Pruebas

```bash
dotnet test backend            # pruebas de la API
cd frontend && npx ng test     # pruebas del frontend
```

### Nueva migración de base de datos

```bash
cd backend
dotnet tool restore
dotnet ef migrations add NombreDelCambio -p src/EcosistemaScout.Api -o Datos/Migraciones
```

## Integrantes y roles

| Integrante | Rol |
|---|---|
| Alonso Salas | Jefe de proyecto y analista funcional; módulos Financiamiento y Portal de pagos |
| Ariel Rios | Arquitecto, datos e infraestructura; módulos Base, Gestión de Miembros e Inventario |

Capstone PTY4614, sección 004D, Ingeniería en Informática, Duoc UC.

## Metodología

Tradicional en cascada con entrega de un MVP: análisis → diseño → construcción → pruebas → entrega. Cada fase cierra con un documento revisado. Los cambios a la línea base se registran en una matriz de control de cambios. En Git usamos una rama por módulo, pull requests revisados por el otro integrante y mensajes con Conventional Commits.

## Arquitectura

Aplicación web con frontend Angular (SPA) y una API .NET organizada como monolito modular, con una instancia por grupo: cada grupo tiene su propia aplicación y base de datos, levantadas desde las mismas imágenes Docker.

```
Navegador ──> nginx (frontend Angular) ──/api──> API .NET 10 ──> PostgreSQL 18
                  │                                   │
                  └── archivos estáticos de la SPA    └── autenticación JWT (ASP.NET Identity)
```

- **Frontend:** una sola aplicación Angular con una *shell* común (barra superior y menú lateral) y un módulo por carpeta en `src/app/features/`, cargado bajo demanda.
- **Backend:** módulos en `Modulos/Base`, `Miembros`, `Inventario`, `Finanzas` y `Portal`. Entity Framework Core mapea las 28 tablas de `database/schema.sql` (más las tablas auxiliares de Identity) y las crea mediante migraciones.
- **Repositorio único (monorepo):** backend y frontend viven juntos para que cada funcionalidad se revise en un solo pull request.

## Créditos

Videos de fondo de la pantalla de inicio de sesión, obtenidos de [Pexels](https://www.pexels.com) bajo la [licencia Pexels](https://www.pexels.com/license/) (uso gratuito, se permite modificarlos). Fueron recortados a 8 segundos y comprimidos en dos versiones: 1080p para computador y 720p para celular:

| Paisaje | Autor | Fuente |
|---|---|---|
| Torres del Paine | Marek Piwnicki | https://www.pexels.com/video/epic-aerial-view-of-torres-del-paine-mountains-39730323/ |
| Desierto de Atacama | Marek Piwnicki | https://www.pexels.com/video/breathtaking-aerial-view-of-atacama-desert-40449624/ |
| Volcán Osorno | Videas Cl | https://www.pexels.com/video/dramatic-view-of-osorno-volcano-in-chile-32773278/ |
| Costa de Algarrobo | Esteban Llanos | https://www.pexels.com/video/ola-del-oceano-pacifico-17489318/ |

## Documentación

- [Convenciones del proyecto](docs/convenciones.md): interfaz, manejo de errores, seguridad, backend y Git.
- [API](docs/api.md): convención de rutas, formato de respuesta, permisos por rol y lista de endpoints.
- [Modelo de datos](docs/modelo-datos.md): diagrama entidad-relación y descripción de cada tabla.
- [Evidencias por etapa](docs/evidencias/): capturas del sistema funcionando y resultados de las pruebas.

## Licencia

Software propietario. © 2026 Ariel Rios y Alonso Salas. Todos los derechos reservados. Ver [LICENSE](LICENSE).

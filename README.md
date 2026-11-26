# Ecosistema Digital Scout

Sistema web de gestión de miembros, inventario y financiamiento para grupos de la Asociación de Guías y Scouts de Chile (AGSCh), con un portal donde los apoderados revisan sus cuotas e informan pagos por transferencia.

## Qué problema resuelve

En los grupos scout la información administrativa vive en planillas, papeles y grupos de WhatsApp. Los pagos llegan como fotos de comprobantes, la tesorería los cruza a mano con la cartola y los vuelve a escribir en su libro; el equipamiento no tiene registro de préstamos y las fichas de salud de los menores quedan en celulares personales. Este sistema registra cada dato una sola vez y genera desde ahí el estado de cuenta de cada familia, el libro de control mensual (DAF-FOR-02) y el inventario (DAF-FOR-04).

**Dirigido a:** tesorería y Comité de apoderados, dirigentes, administradores de grupo y apoderados.

## Módulos del MVP

- **Base**: Inicio de sesión, usuarios, roles acumulables con alcance por unidad, configuración del grupo y bitácora de auditoría.
- **Gestión de Miembros**: Unidades y patrullas, beneficiarios, apoderados y sus vínculos, contactos de emergencia, ficha médica cifrada, voluntarios y cargos, historial de cada beneficiario.
- **Inventario**: Bienes con los campos del formulario DAF-FOR-04, préstamos y devoluciones, bajas con motivo y exportación a Excel.
- **Financiamiento**: Plan de cuentas, ingresos y egresos con comprobante correlativo, centros de costo, cuotas y cargos por beneficiario, estado de cuenta y libro de control mensual.
- **Portal de pagos**: Vista del apoderado con sus cargos, datos bancarios del grupo, informe de transferencias con comprobante, revisión por tesorería y recibo en PDF. Solo transferencias; sin pago en línea.

Fuera del alcance: pago en línea, eventos y campamentos, gestión documental y comunicaciones.

## Tecnologías

- PHP 8.3 y Laravel 11 (monolito modular)
- Blade y Tailwind CSS (interfaz adaptable al celular)
- PostgreSQL 16
- Docker y Docker Compose (nginx, app, worker, db; Mailpit en desarrollo)
- Pest/PHPUnit, k6 y OWASP ZAP para pruebas

## Cómo ejecutarlo localmente

Requisitos: Docker Desktop y Git.

```bash
git clone https://github.com/USUARIO/ecosistema-scout.git
cd ecosistema-scout
cp .env.example .env
docker compose --profile dev up -d --build
docker compose exec app php artisan key:generate
docker compose exec app php artisan migrate --seed
```

- Aplicación: http://localhost:8080
- Correos de prueba (Mailpit): http://localhost:8025
- Usuarios de demostración (contraseña `password`): admin@demo.cl, tesoreria@demo.cl, dirigente@demo.cl, apoderado@demo.cl, revisor@demo.cl

Pruebas: `docker compose exec app php artisan test`

## Integrantes y roles

| Integrante | Rol |
|---|---|
| Alonso Salas | Jefe de proyecto y analista funcional; módulos Financiamiento y Portal de pagos |
| Ariel Rios | Arquitecto, datos e infraestructura; módulos Base, Gestión de Miembros e Inventario |

Capstone PTY4614, sección 004D, Ingeniería en Informática, Duoc UC.

## Metodología

Tradicional en cascada con entrega de un MVP: análisis → diseño → construcción → pruebas → entrega. Cada fase cierra con un documento revisado (carpeta `docs/`). Los cambios a la línea base se registran en una matriz de control de cambios. En Git usamos una rama por módulo, pull requests revisados por el otro integrante y mensajes con Conventional Commits.

## Arquitectura

Monolito modular en capas (presentación, lógica de negocio por módulo y acceso a datos) con una instancia por grupo: cada grupo tiene su propia aplicación, base de datos y archivos, levantados desde la misma imagen Docker.

```
Navegador ──HTTPS──> nginx ──FastCGI──> app (Laravel) ──> PostgreSQL
                                          │
                                          ├──> volumen de archivos (comprobantes, fotos)
                                          └──> worker (cola de correos y PDF) ──> SMTP
```

Módulos: `app/Modules/Base`, `Miembros`, `Inventario`, `Finanzas`, `Portal`. El modelo de datos (28 tablas) está en `database/schema.sql` y en el Documento de Diseño.

## Documentación

- `docs/01_Documento_de_Inicio_y_Planificacion.docx`
- `docs/02_ERS_Especificacion_de_Requisitos.docx`
- `docs/03_Documento_de_Diseno.docx`
- `docs/04_Plan_de_Pruebas.docx`
- `docs/05_Manual_Tecnico_y_Despliegue.docx`

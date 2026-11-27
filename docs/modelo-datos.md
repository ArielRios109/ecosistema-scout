# Modelo de datos

Base de datos PostgreSQL 18 con **28 tablas del dominio**, agrupadas en 5 módulos, más las tablas de soporte de ASP.NET Identity y de Data Protection. Las tablas las crean las **migraciones de Entity Framework Core** (`backend/src/EcosistemaScout.Api/Datos/Migraciones`) al arrancar la API. `database/schema.sql` es el modelo de referencia original.

Convenciones:

- nombres en español y en `snake_case`;
- `id bigint` autoincremental como clave primaria;
- `created_at` / `updated_at` en UTC;
- montos en pesos chilenos como enteros;
- valores fijos validados con `CHECK` (rama, estado, tipo...).

## Cómo ver el diagrama

- **GitHub o VS Code** muestran los diagramas Mermaid de este archivo. En VS Code, usa la vista previa de Markdown (`Ctrl+Shift+V`) con la extensión *Markdown Preview Mermaid Support*.
- **Diagrama completo con todas las columnas:** con Docker levantado, conéctate desde DBeaver o pgAdmin a `localhost:5433`, base de datos `ecosistema_scout` (los valores de desarrollo están en `docker-compose.yml`). Luego abre *ER Diagram* sobre el esquema `public`.

## Vista general

```mermaid
erDiagram
    usuarios ||--o{ usuario_rol : tiene
    roles ||--o{ usuario_rol : asigna
    unidades ||--o{ usuario_rol : "alcance (opcional)"
    usuarios ||--o{ bitacora : registra

    unidades ||--o{ patrullas : agrupa
    unidades ||--o{ beneficiarios : integra
    patrullas |o--o{ beneficiarios : "integra (opcional)"
    beneficiarios ||--o| fichas_medicas : tiene
    beneficiarios ||--o{ contactos_emergencia : tiene
    beneficiarios ||--o{ beneficiario_apoderado : ""
    apoderados ||--o{ beneficiario_apoderado : ""
    beneficiarios ||--o{ historial_beneficiario : registra
    voluntarios ||--o{ cargos_voluntario : ejerce
    unidades |o--o{ cargos_voluntario : "en (opcional)"
    usuarios |o--o| apoderados : "cuenta del Portal"
    usuarios |o--o| voluntarios : "cuenta"

    categorias_bien ||--o{ bienes : clasifica
    voluntarios |o--o{ bienes : "responsable"
    bienes ||--o{ prestamos : presta
    bienes ||--o{ bajas_bien : "da de baja"

    items_presupuestarios ||--o{ movimientos : clasifica
    centros_costo ||--o{ movimientos : imputa
    unidades |o--o{ centros_costo : "de la unidad"
    items_presupuestarios ||--o{ conceptos_cobro : clasifica

    conceptos_cobro ||--o{ cargos : origina
    beneficiarios ||--o{ cargos : debe
    centros_costo ||--o{ cargos : imputa
    cargos ||--o{ abonos : "se paga con"
    movimientos ||--o{ abonos : genera
    apoderados ||--o{ pagos_informados : informa
    pagos_informados ||--o{ pago_informado_cargo : aplica
    cargos ||--o{ pago_informado_cargo : recibe
    movimientos |o--o| pagos_informados : "al aprobarse"
```

## Módulo Base

```mermaid
erDiagram
    usuarios {
        bigint id PK
        varchar nombre
        varchar email UK
        boolean activo
        boolean debe_cambiar_password
        timestamptz ultimo_acceso
    }
    roles {
        bigint id PK
        varchar nombre UK
        varchar descripcion
    }
    usuario_rol {
        bigint usuario_id PK,FK
        bigint rol_id PK,FK
        bigint unidad_id FK "opcional"
    }
    bitacora {
        bigint id PK
        bigint usuario_id FK
        varchar accion
        varchar entidad
        bigint entidad_id
        jsonb antes
        jsonb despues
        varchar ip
    }
    usuarios ||--o{ usuario_rol : ""
    roles ||--o{ usuario_rol : ""
    usuarios ||--o{ bitacora : ""
```

| Tabla | Para qué sirve |
|---|---|
| `configuracion_grupo` | Datos del grupo: nombre, número, distrito y zona, cuenta bancaria para las transferencias y logo |
| `usuarios` | Cuentas de acceso (ASP.NET Identity). `debe_cambiar_password` obliga a cambiar la contraseña temporal |
| `roles` | administrador, tesorería, dirigente, apoderado y revisor |
| `usuario_rol` | Roles de cada usuario (acumulables), con unidad opcional para limitar el alcance a futuro |
| `bitacora` | Auditoría automática: quién, cuándo, desde qué IP y el antes y después de cada cambio. Los datos de salud quedan como `"(dato protegido)"` |

## Módulo Gestión de Miembros

```mermaid
erDiagram
    unidades {
        bigint id PK
        varchar nombre
        varchar rama "golondrinas, lobatos, guias, scouts, pioneros o caminantes"
        boolean activa
    }
    patrullas {
        bigint id PK
        bigint unidad_id FK
        varchar nombre "único por unidad"
        boolean activa
    }
    beneficiarios {
        bigint id PK
        varchar rut UK
        varchar nombres
        varchar apellidos
        date fecha_nacimiento
        bigint unidad_id FK
        bigint patrulla_id FK
        date fecha_ingreso
        varchar estado "activo o retirado"
    }
    fichas_medicas {
        bigint id PK
        bigint beneficiario_id FK,UK
        text alergias "cifrado"
        text enfermedades "cifrado"
        text medicamentos "cifrado"
        bigint actualizado_por FK "SET NULL"
    }
    contactos_emergencia {
        bigint id PK
        bigint beneficiario_id FK
        varchar nombre
        varchar telefono
    }
    apoderados {
        bigint id PK
        varchar rut UK
        varchar email
        bigint usuario_id FK,UK "cuenta del Portal"
    }
    beneficiario_apoderado {
        bigint beneficiario_id PK,FK
        bigint apoderado_id PK,FK
        varchar parentesco
        boolean es_principal
    }
    historial_beneficiario {
        bigint id PK
        bigint beneficiario_id FK
        varchar tipo "ingreso, cambio_unidad, cambio_patrulla, retiro o reingreso"
        bigint unidad_origen_id FK
        bigint unidad_destino_id FK
        date fecha
        bigint usuario_id FK
    }
    voluntarios {
        bigint id PK
        varchar rut UK
        bigint usuario_id FK,UK
    }
    cargos_voluntario {
        bigint id PK
        bigint voluntario_id FK
        bigint unidad_id FK "opcional"
        varchar cargo
        date fecha_inicio
        date fecha_termino "vacía = vigente"
    }
    unidades ||--o{ patrullas : ""
    unidades ||--o{ beneficiarios : ""
    patrullas |o--o{ beneficiarios : ""
    beneficiarios ||--o| fichas_medicas : ""
    beneficiarios ||--o{ contactos_emergencia : ""
    beneficiarios ||--o{ beneficiario_apoderado : ""
    apoderados ||--o{ beneficiario_apoderado : ""
    beneficiarios ||--o{ historial_beneficiario : ""
    voluntarios ||--o{ cargos_voluntario : ""
    unidades |o--o{ cargos_voluntario : ""
```

| Tabla | Para qué sirve |
|---|---|
| `unidades` | Unidades del grupo, una rama por unidad. No se eliminan si tienen beneficiarios: se desactivan |
| `patrullas` | Subdivisiones de una unidad (patrulla, seisena, equipo). El nombre no se repite dentro de la unidad |
| `beneficiarios` | Niños, niñas y jóvenes. RUT único, guardado sin puntos (`12345678-5`). Nunca se borran: se retiran |
| `fichas_medicas` | Previsión, grupo sanguíneo, alergias, enfermedades, medicamentos, restricciones alimentarias y necesidades emocionales, **cifradas** con ASP.NET Data Protection |
| `contactos_emergencia` | Hasta 5 personas a quienes llamar por cada beneficiario |
| `apoderados` | Adultos responsables. `usuario_id` enlaza su cuenta del Portal de pagos |
| `beneficiario_apoderado` | Vínculo de muchos a muchos con parentesco y un único apoderado principal por beneficiario |
| `historial_beneficiario` | Registro automático de ingresos, cambios de unidad o patrulla, retiros y reingresos |
| `voluntarios` | Dirigentes y adultos colaboradores; pueden tener cuenta de usuario |
| `cargos_voluntario` | Cargos de grupo o de unidad, con fechas. Los terminados quedan como historial |

## Módulo Inventario

| Tabla | Para qué sirve |
|---|---|
| `categorias_bien` | Clasificación de los bienes (carpas, cocina, herramientas...) |
| `bienes` | Ítems con los campos del formulario DAF-FOR-04: código, cantidad, origen, estado, ubicación, responsable y foto |
| `prestamos` | Salidas y devoluciones, con destinatario, actividad y estado al volver |
| `bajas_bien` | Bajas con motivo (pérdida, deterioro, donación...) |

## Módulo Financiamiento

| Tabla | Para qué sirve |
|---|---|
| `items_presupuestarios` | Plan de cuentas de ingresos y egresos |
| `centros_costo` | Grupo o unidad a la que se imputa cada movimiento |
| `movimientos` | Ingresos y egresos con comprobante correlativo por año. Se anulan, nunca se borran |
| `cierres_mensuales` | Saldos de cada mes cerrado (libro de control DAF-FOR-02) |
| `conceptos_cobro` | Cuotas y cobros (cuota anual, campamento...) con monto sugerido |
| `cargos` | Deuda de cada beneficiario por concepto, con vencimiento y estado |
| `abonos` | Pagos aplicados a un cargo, ligados al movimiento de ingreso |

## Módulo Portal de pagos

| Tabla | Para qué sirve |
|---|---|
| `pagos_informados` | Transferencias que informa el apoderado, con comprobante. Tesorería las aprueba o las rechaza |
| `pago_informado_cargo` | Cómo se reparte una transferencia entre los cargos que paga |

## Tablas de soporte

| Tabla | Para qué sirve |
|---|---|
| `usuario_claims`, `usuario_logins`, `usuario_tokens`, `rol_claims` | Tablas internas de ASP.NET Identity |
| `data_protection_keys` | Llaves de cifrado de la ficha médica. Se guardan en la base de datos para que sobrevivan a los reinicios. **Excluida de la bitácora** |
| `__EFMigrationsHistory` | Migraciones aplicadas |

## Cambios respecto de `schema.sql`

| Cambio | Motivo |
|---|---|
| `usuarios.debe_cambiar_password` | Forzar el cambio de la contraseña temporal en el primer ingreso |
| `fichas_medicas.actualizado_por` pasa de `ON DELETE CASCADE` a `SET NULL` | Eliminar la cuenta de quien editó una ficha no debe borrar la ficha del beneficiario |
| `fichas_medicas.prevision` y `grupo_sanguineo` pasan de `varchar(40)`/`varchar(5)` a `text` | Se guardan cifradas y el texto cifrado es más largo; la API valida el largo antes de cifrar |
| Nueva tabla `data_protection_keys` | Persistir las llaves de cifrado |

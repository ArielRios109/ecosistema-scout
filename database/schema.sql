-- Ecosistema Digital Scout · Script de creación de la base de datos (PostgreSQL 16)
-- Generado desde el modelo de datos del Documento de Diseño. Una base de datos por grupo (instancia).
-- Ejecutar: psql -U scout -d scout -f schema.sql

BEGIN;

-- ===== Módulo Base =====
-- Datos del grupo dueño de la instancia (una sola fila).
CREATE TABLE configuracion_grupo (
    id BIGSERIAL,
    nombre_grupo VARCHAR(120) NOT NULL,
    numero_grupo VARCHAR(20) NULL,
    distrito VARCHAR(80) NULL,
    zona VARCHAR(80) NULL,
    banco VARCHAR(60) NULL,
    tipo_cuenta VARCHAR(30) NULL,
    numero_cuenta VARCHAR(30) NULL,
    titular_cuenta VARCHAR(120) NULL,
    rut_titular VARCHAR(12) NULL,
    email_tesoreria VARCHAR(120) NULL,
    logo_path VARCHAR(255) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Personas que ingresan al sistema.
CREATE TABLE usuarios (
    id BIGSERIAL,
    nombre VARCHAR(120) NOT NULL,
    email VARCHAR(120) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT true,
    ultimo_acceso TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Roles del sistema: administrador, tesoreria, dirigente, apoderado, revisor.
CREATE TABLE roles (
    id BIGSERIAL,
    nombre VARCHAR(40) NOT NULL UNIQUE,
    descripcion VARCHAR(200) NULL,
    PRIMARY KEY (id)
);

-- Asignación de roles (acumulables) con alcance opcional a una unidad.
CREATE TABLE usuario_rol (
    id BIGSERIAL,
    usuario_id BIGINT NOT NULL,
    rol_id BIGINT NOT NULL,
    unidad_id BIGINT NULL,
    PRIMARY KEY (id),
    UNIQUE (usuario_id, rol_id, unidad_id)
);

-- Registro de auditoría de toda creación, modificación, aprobación y anulación.
CREATE TABLE bitacora (
    id BIGSERIAL,
    usuario_id BIGINT NULL,
    accion VARCHAR(30) NOT NULL,
    entidad VARCHAR(60) NOT NULL,
    entidad_id BIGINT NULL,
    antes JSONB NULL,
    despues JSONB NULL,
    ip VARCHAR(45) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- ===== Módulo Miembros =====
-- Unidades del grupo y su rama.
CREATE TABLE unidades (
    id BIGSERIAL,
    nombre VARCHAR(80) NOT NULL,
    rama VARCHAR(30) NOT NULL CHECK (rama IN ('golondrinas','lobatos','guias','scouts','pioneros','caminantes')),
    activa BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Subdivisiones de una unidad.
CREATE TABLE patrullas (
    id BIGSERIAL,
    unidad_id BIGINT NOT NULL,
    nombre VARCHAR(80) NOT NULL,
    activa BOOLEAN NOT NULL DEFAULT true,
    PRIMARY KEY (id),
    UNIQUE (unidad_id, nombre)
);

-- Niños, niñas y jóvenes del grupo.
CREATE TABLE beneficiarios (
    id BIGSERIAL,
    rut VARCHAR(12) NOT NULL UNIQUE,
    nombres VARCHAR(80) NOT NULL,
    apellidos VARCHAR(80) NOT NULL,
    fecha_nacimiento DATE NOT NULL,
    direccion VARCHAR(200) NULL,
    unidad_id BIGINT NOT NULL,
    patrulla_id BIGINT NULL,
    fecha_ingreso DATE NOT NULL,
    estado VARCHAR(15) NOT NULL DEFAULT 'activo' CHECK (estado IN ('activo','retirado')),
    registro_institucional_anio SMALLINT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Ficha médica y de necesidades emocionales; los campos sensibles se guardan cifrados (AES-256).
CREATE TABLE fichas_medicas (
    id BIGSERIAL,
    beneficiario_id BIGINT NOT NULL UNIQUE,
    prevision VARCHAR(40) NULL,
    grupo_sanguineo VARCHAR(5) NULL,
    alergias TEXT NULL,
    enfermedades TEXT NULL,
    medicamentos TEXT NULL,
    restricciones_alimentarias TEXT NULL,
    necesidades_emocionales TEXT NULL,
    actualizado_por BIGINT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Contactos de emergencia de un beneficiario.
CREATE TABLE contactos_emergencia (
    id BIGSERIAL,
    beneficiario_id BIGINT NOT NULL,
    nombre VARCHAR(120) NOT NULL,
    parentesco VARCHAR(40) NULL,
    telefono VARCHAR(20) NOT NULL,
    PRIMARY KEY (id)
);

-- Adultos responsables de uno o más beneficiarios.
CREATE TABLE apoderados (
    id BIGSERIAL,
    usuario_id BIGINT NULL UNIQUE,
    rut VARCHAR(12) NOT NULL UNIQUE,
    nombres VARCHAR(80) NOT NULL,
    apellidos VARCHAR(80) NOT NULL,
    email VARCHAR(120) NOT NULL,
    telefono VARCHAR(20) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Relación N:M entre beneficiarios y apoderados.
CREATE TABLE beneficiario_apoderado (
    beneficiario_id BIGINT NOT NULL,
    apoderado_id BIGINT NOT NULL,
    parentesco VARCHAR(40) NOT NULL,
    es_principal BOOLEAN NOT NULL DEFAULT false,
    PRIMARY KEY (beneficiario_id, apoderado_id)
);

-- Dirigentes, guiadoras y otros adultos con cargo.
CREATE TABLE voluntarios (
    id BIGSERIAL,
    usuario_id BIGINT NULL UNIQUE,
    rut VARCHAR(12) NOT NULL UNIQUE,
    nombres VARCHAR(80) NOT NULL,
    apellidos VARCHAR(80) NOT NULL,
    email VARCHAR(120) NULL,
    telefono VARCHAR(20) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Cargos que ocupa un voluntario, con período.
CREATE TABLE cargos_voluntario (
    id BIGSERIAL,
    voluntario_id BIGINT NOT NULL,
    unidad_id BIGINT NULL,
    cargo VARCHAR(60) NOT NULL,
    fecha_inicio DATE NOT NULL,
    fecha_termino DATE NULL,
    PRIMARY KEY (id)
);

-- Ingresos, cambios de unidad o patrulla y retiros.
CREATE TABLE historial_beneficiario (
    id BIGSERIAL,
    beneficiario_id BIGINT NOT NULL,
    tipo VARCHAR(20) NOT NULL CHECK (tipo IN ('ingreso','cambio_unidad','cambio_patrulla','retiro','reingreso')),
    unidad_origen_id BIGINT NULL,
    unidad_destino_id BIGINT NULL,
    fecha DATE NOT NULL,
    detalle VARCHAR(255) NULL,
    usuario_id BIGINT NULL,
    PRIMARY KEY (id)
);

-- ===== Módulo Inventario =====
-- Categorías de bienes (carpas, cocina, herramientas, etc.).
CREATE TABLE categorias_bien (
    id BIGSERIAL,
    nombre VARCHAR(60) NOT NULL UNIQUE,
    PRIMARY KEY (id)
);

-- Bienes del grupo con los campos del formulario DAF-FOR-04.
CREATE TABLE bienes (
    id BIGSERIAL,
    codigo VARCHAR(20) NOT NULL UNIQUE,
    nombre VARCHAR(120) NOT NULL,
    categoria_id BIGINT NOT NULL,
    cantidad INTEGER NOT NULL DEFAULT 1 CHECK (cantidad >= 0),
    marca VARCHAR(60) NULL,
    numero_serie VARCHAR(60) NULL,
    descripcion TEXT NULL,
    fecha_adquisicion DATE NULL,
    precio_compra INTEGER NULL CHECK (precio_compra >= 0),
    valor_estimado INTEGER NULL CHECK (valor_estimado >= 0),
    origen VARCHAR(15) NOT NULL CHECK (origen IN ('propio','prestamo','consignacion','comodato','donacion')),
    estado VARCHAR(15) NOT NULL CHECK (estado IN ('bueno','regular','malo','reparacion')),
    ubicacion VARCHAR(120) NULL,
    responsable_id BIGINT NULL,
    foto_path VARCHAR(255) NULL,
    dado_de_baja BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Salidas de bienes a personas o actividades, con devolución.
CREATE TABLE prestamos (
    id BIGSERIAL,
    bien_id BIGINT NOT NULL,
    cantidad INTEGER NOT NULL CHECK (cantidad > 0),
    destinatario VARCHAR(120) NOT NULL,
    actividad VARCHAR(120) NULL,
    fecha_salida DATE NOT NULL,
    fecha_devolucion_esperada DATE NOT NULL,
    fecha_devolucion DATE NULL,
    estado_devolucion VARCHAR(15) NULL,
    registrado_por BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Bajas de inventario (DAF-FOR-09); el bien no se borra.
CREATE TABLE bajas_bien (
    id BIGSERIAL,
    bien_id BIGINT NOT NULL UNIQUE,
    motivo VARCHAR(15) NOT NULL CHECK (motivo IN ('venta','robo','donacion','deterioro','perdida','otro')),
    fecha DATE NOT NULL,
    detalle VARCHAR(255) NULL,
    registrado_por BIGINT NOT NULL,
    PRIMARY KEY (id)
);

-- ===== Módulo Financiamiento =====
-- Plan de cuentas basado en el Presupuesto Grupal DAF-FOR-07.
CREATE TABLE items_presupuestarios (
    id BIGSERIAL,
    codigo VARCHAR(10) NOT NULL UNIQUE,
    nombre VARCHAR(120) NOT NULL,
    tipo VARCHAR(7) NOT NULL CHECK (tipo IN ('ingreso','egreso')),
    activo BOOLEAN NOT NULL DEFAULT true,
    PRIMARY KEY (id)
);

-- Separación del dinero por grupo, unidad o actividad.
CREATE TABLE centros_costo (
    id BIGSERIAL,
    nombre VARCHAR(120) NOT NULL,
    tipo VARCHAR(10) NOT NULL CHECK (tipo IN ('grupo','unidad','actividad')),
    unidad_id BIGINT NULL,
    activo BOOLEAN NOT NULL DEFAULT true,
    PRIMARY KEY (id)
);

-- Ingresos y egresos de dinero; nunca se borran, solo se anulan.
CREATE TABLE movimientos (
    id BIGSERIAL,
    numero_comprobante INTEGER NOT NULL,
    anio SMALLINT NOT NULL,
    tipo VARCHAR(7) NOT NULL CHECK (tipo IN ('ingreso','egreso')),
    fecha DATE NOT NULL,
    item_id BIGINT NOT NULL,
    centro_costo_id BIGINT NOT NULL,
    monto INTEGER NOT NULL CHECK (monto > 0),
    medio_pago VARCHAR(15) NOT NULL CHECK (medio_pago IN ('efectivo','transferencia','otro')),
    tipo_documento VARCHAR(15) NULL,
    numero_documento VARCHAR(30) NULL,
    descripcion VARCHAR(255) NOT NULL,
    respaldo_path VARCHAR(255) NULL,
    estado VARCHAR(8) NOT NULL DEFAULT 'vigente' CHECK (estado IN ('vigente','anulado')),
    motivo_anulacion VARCHAR(255) NULL,
    anulado_por BIGINT NULL,
    anulado_en TIMESTAMPTZ NULL,
    registrado_por BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id),
    UNIQUE (tipo, anio, numero_comprobante)
);

-- Meses cerrados: sus movimientos ya no se editan.
CREATE TABLE cierres_mensuales (
    id BIGSERIAL,
    anio SMALLINT NOT NULL,
    mes SMALLINT NOT NULL CHECK (mes BETWEEN 1 AND 12),
    saldo_inicial BIGINT NOT NULL,
    total_ingresos BIGINT NOT NULL,
    total_egresos BIGINT NOT NULL,
    saldo_final BIGINT NOT NULL,
    cerrado_por BIGINT NOT NULL,
    cerrado_en TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id),
    UNIQUE (anio, mes)
);

-- Tipos de cobro: cuota mensual, registro anual, campamento, actividad.
CREATE TABLE conceptos_cobro (
    id BIGSERIAL,
    nombre VARCHAR(120) NOT NULL,
    tipo VARCHAR(12) NOT NULL CHECK (tipo IN ('mensual','anual','campamento','actividad','venta','otro')),
    item_id BIGINT NOT NULL,
    monto_sugerido INTEGER NULL CHECK (monto_sugerido >= 0),
    PRIMARY KEY (id)
);

-- Deuda de un beneficiario por un concepto, con vencimiento.
CREATE TABLE cargos (
    id BIGSERIAL,
    beneficiario_id BIGINT NOT NULL,
    concepto_id BIGINT NOT NULL,
    centro_costo_id BIGINT NOT NULL,
    descripcion VARCHAR(160) NOT NULL,
    monto INTEGER NOT NULL CHECK (monto > 0),
    fecha_vencimiento DATE NOT NULL,
    estado VARCHAR(10) NOT NULL DEFAULT 'pendiente' CHECK (estado IN ('pendiente','parcial','pagado','anulado')),
    creado_por BIGINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- Aplicación de un ingreso a uno o más cargos.
CREATE TABLE abonos (
    id BIGSERIAL,
    cargo_id BIGINT NOT NULL,
    movimiento_id BIGINT NOT NULL,
    monto INTEGER NOT NULL CHECK (monto > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

-- ===== Módulo Portal de pagos =====
-- Transferencias informadas por apoderados, pendientes de revisión.
CREATE TABLE pagos_informados (
    id BIGSERIAL,
    apoderado_id BIGINT NOT NULL,
    fecha_transferencia DATE NOT NULL,
    monto INTEGER NOT NULL CHECK (monto > 0),
    banco_origen VARCHAR(60) NOT NULL,
    numero_operacion VARCHAR(40) NOT NULL,
    comprobante_path VARCHAR(255) NOT NULL,
    estado VARCHAR(10) NOT NULL DEFAULT 'pendiente' CHECK (estado IN ('pendiente','aprobado','rechazado')),
    motivo_rechazo VARCHAR(255) NULL,
    revisado_por BIGINT NULL,
    revisado_en TIMESTAMPTZ NULL,
    movimiento_id BIGINT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (id),
    UNIQUE (banco_origen, numero_operacion)
);

-- Cargos que el apoderado declara pagar con una transferencia.
CREATE TABLE pago_informado_cargo (
    pago_informado_id BIGINT NOT NULL,
    cargo_id BIGINT NOT NULL,
    monto_aplicado INTEGER NOT NULL CHECK (monto_aplicado > 0),
    PRIMARY KEY (pago_informado_id, cargo_id)
);

-- ===== Claves foráneas =====
ALTER TABLE usuario_rol ADD CONSTRAINT fk_usuario_rol_usuario_id FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE;
ALTER TABLE usuario_rol ADD CONSTRAINT fk_usuario_rol_rol_id FOREIGN KEY (rol_id) REFERENCES roles(id);
ALTER TABLE usuario_rol ADD CONSTRAINT fk_usuario_rol_unidad_id FOREIGN KEY (unidad_id) REFERENCES unidades(id);
ALTER TABLE bitacora ADD CONSTRAINT fk_bitacora_usuario_id FOREIGN KEY (usuario_id) REFERENCES usuarios(id);
ALTER TABLE patrullas ADD CONSTRAINT fk_patrullas_unidad_id FOREIGN KEY (unidad_id) REFERENCES unidades(id);
ALTER TABLE beneficiarios ADD CONSTRAINT fk_beneficiarios_unidad_id FOREIGN KEY (unidad_id) REFERENCES unidades(id);
ALTER TABLE beneficiarios ADD CONSTRAINT fk_beneficiarios_patrulla_id FOREIGN KEY (patrulla_id) REFERENCES patrullas(id);
ALTER TABLE fichas_medicas ADD CONSTRAINT fk_fichas_medicas_beneficiario_id FOREIGN KEY (beneficiario_id) REFERENCES beneficiarios(id) ON DELETE CASCADE;
ALTER TABLE fichas_medicas ADD CONSTRAINT fk_fichas_medicas_actualizado_por FOREIGN KEY (actualizado_por) REFERENCES usuarios(id) ON DELETE CASCADE;
ALTER TABLE contactos_emergencia ADD CONSTRAINT fk_contactos_emergencia_beneficiario_id FOREIGN KEY (beneficiario_id) REFERENCES beneficiarios(id) ON DELETE CASCADE;
ALTER TABLE apoderados ADD CONSTRAINT fk_apoderados_usuario_id FOREIGN KEY (usuario_id) REFERENCES usuarios(id);
ALTER TABLE beneficiario_apoderado ADD CONSTRAINT fk_beneficiario_apoderado_beneficiario_id FOREIGN KEY (beneficiario_id) REFERENCES beneficiarios(id) ON DELETE CASCADE;
ALTER TABLE beneficiario_apoderado ADD CONSTRAINT fk_beneficiario_apoderado_apoderado_id FOREIGN KEY (apoderado_id) REFERENCES apoderados(id) ON DELETE CASCADE;
ALTER TABLE voluntarios ADD CONSTRAINT fk_voluntarios_usuario_id FOREIGN KEY (usuario_id) REFERENCES usuarios(id);
ALTER TABLE cargos_voluntario ADD CONSTRAINT fk_cargos_voluntario_voluntario_id FOREIGN KEY (voluntario_id) REFERENCES voluntarios(id);
ALTER TABLE cargos_voluntario ADD CONSTRAINT fk_cargos_voluntario_unidad_id FOREIGN KEY (unidad_id) REFERENCES unidades(id);
ALTER TABLE historial_beneficiario ADD CONSTRAINT fk_historial_beneficiario_beneficiario_id FOREIGN KEY (beneficiario_id) REFERENCES beneficiarios(id);
ALTER TABLE historial_beneficiario ADD CONSTRAINT fk_historial_beneficiario_unidad_origen_id FOREIGN KEY (unidad_origen_id) REFERENCES unidades(id);
ALTER TABLE historial_beneficiario ADD CONSTRAINT fk_historial_beneficiario_unidad_destino_id FOREIGN KEY (unidad_destino_id) REFERENCES unidades(id);
ALTER TABLE historial_beneficiario ADD CONSTRAINT fk_historial_beneficiario_usuario_id FOREIGN KEY (usuario_id) REFERENCES usuarios(id);
ALTER TABLE bienes ADD CONSTRAINT fk_bienes_categoria_id FOREIGN KEY (categoria_id) REFERENCES categorias_bien(id);
ALTER TABLE bienes ADD CONSTRAINT fk_bienes_responsable_id FOREIGN KEY (responsable_id) REFERENCES voluntarios(id);
ALTER TABLE prestamos ADD CONSTRAINT fk_prestamos_bien_id FOREIGN KEY (bien_id) REFERENCES bienes(id);
ALTER TABLE prestamos ADD CONSTRAINT fk_prestamos_registrado_por FOREIGN KEY (registrado_por) REFERENCES usuarios(id);
ALTER TABLE bajas_bien ADD CONSTRAINT fk_bajas_bien_bien_id FOREIGN KEY (bien_id) REFERENCES bienes(id);
ALTER TABLE bajas_bien ADD CONSTRAINT fk_bajas_bien_registrado_por FOREIGN KEY (registrado_por) REFERENCES usuarios(id);
ALTER TABLE centros_costo ADD CONSTRAINT fk_centros_costo_unidad_id FOREIGN KEY (unidad_id) REFERENCES unidades(id);
ALTER TABLE movimientos ADD CONSTRAINT fk_movimientos_item_id FOREIGN KEY (item_id) REFERENCES items_presupuestarios(id);
ALTER TABLE movimientos ADD CONSTRAINT fk_movimientos_centro_costo_id FOREIGN KEY (centro_costo_id) REFERENCES centros_costo(id);
ALTER TABLE movimientos ADD CONSTRAINT fk_movimientos_anulado_por FOREIGN KEY (anulado_por) REFERENCES usuarios(id);
ALTER TABLE movimientos ADD CONSTRAINT fk_movimientos_registrado_por FOREIGN KEY (registrado_por) REFERENCES usuarios(id);
ALTER TABLE cierres_mensuales ADD CONSTRAINT fk_cierres_mensuales_cerrado_por FOREIGN KEY (cerrado_por) REFERENCES usuarios(id);
ALTER TABLE conceptos_cobro ADD CONSTRAINT fk_conceptos_cobro_item_id FOREIGN KEY (item_id) REFERENCES items_presupuestarios(id);
ALTER TABLE cargos ADD CONSTRAINT fk_cargos_beneficiario_id FOREIGN KEY (beneficiario_id) REFERENCES beneficiarios(id);
ALTER TABLE cargos ADD CONSTRAINT fk_cargos_concepto_id FOREIGN KEY (concepto_id) REFERENCES conceptos_cobro(id);
ALTER TABLE cargos ADD CONSTRAINT fk_cargos_centro_costo_id FOREIGN KEY (centro_costo_id) REFERENCES centros_costo(id);
ALTER TABLE cargos ADD CONSTRAINT fk_cargos_creado_por FOREIGN KEY (creado_por) REFERENCES usuarios(id);
ALTER TABLE abonos ADD CONSTRAINT fk_abonos_cargo_id FOREIGN KEY (cargo_id) REFERENCES cargos(id);
ALTER TABLE abonos ADD CONSTRAINT fk_abonos_movimiento_id FOREIGN KEY (movimiento_id) REFERENCES movimientos(id);
ALTER TABLE pagos_informados ADD CONSTRAINT fk_pagos_informados_apoderado_id FOREIGN KEY (apoderado_id) REFERENCES apoderados(id);
ALTER TABLE pagos_informados ADD CONSTRAINT fk_pagos_informados_revisado_por FOREIGN KEY (revisado_por) REFERENCES usuarios(id);
ALTER TABLE pagos_informados ADD CONSTRAINT fk_pagos_informados_movimiento_id FOREIGN KEY (movimiento_id) REFERENCES movimientos(id);
ALTER TABLE pago_informado_cargo ADD CONSTRAINT fk_pago_informado_cargo_pago_informado_id FOREIGN KEY (pago_informado_id) REFERENCES pagos_informados(id) ON DELETE CASCADE;
ALTER TABLE pago_informado_cargo ADD CONSTRAINT fk_pago_informado_cargo_cargo_id FOREIGN KEY (cargo_id) REFERENCES cargos(id) ON DELETE CASCADE;

-- ===== Índices de apoyo =====
CREATE INDEX idx_usuario_rol_usuario_id ON usuario_rol(usuario_id);
CREATE INDEX idx_usuario_rol_rol_id ON usuario_rol(rol_id);
CREATE INDEX idx_usuario_rol_unidad_id ON usuario_rol(unidad_id);
CREATE INDEX idx_bitacora_usuario_id ON bitacora(usuario_id);
CREATE INDEX idx_patrullas_unidad_id ON patrullas(unidad_id);
CREATE INDEX idx_beneficiarios_unidad_id ON beneficiarios(unidad_id);
CREATE INDEX idx_beneficiarios_patrulla_id ON beneficiarios(patrulla_id);
CREATE INDEX idx_fichas_medicas_actualizado_por ON fichas_medicas(actualizado_por);
CREATE INDEX idx_contactos_emergencia_beneficiario_id ON contactos_emergencia(beneficiario_id);
CREATE INDEX idx_cargos_voluntario_voluntario_id ON cargos_voluntario(voluntario_id);
CREATE INDEX idx_cargos_voluntario_unidad_id ON cargos_voluntario(unidad_id);
CREATE INDEX idx_historial_beneficiario_beneficiario_id ON historial_beneficiario(beneficiario_id);
CREATE INDEX idx_historial_beneficiario_unidad_origen_id ON historial_beneficiario(unidad_origen_id);
CREATE INDEX idx_historial_beneficiario_unidad_destino_id ON historial_beneficiario(unidad_destino_id);
CREATE INDEX idx_historial_beneficiario_usuario_id ON historial_beneficiario(usuario_id);
CREATE INDEX idx_bienes_categoria_id ON bienes(categoria_id);
CREATE INDEX idx_bienes_responsable_id ON bienes(responsable_id);
CREATE INDEX idx_prestamos_bien_id ON prestamos(bien_id);
CREATE INDEX idx_prestamos_registrado_por ON prestamos(registrado_por);
CREATE INDEX idx_bajas_bien_registrado_por ON bajas_bien(registrado_por);
CREATE INDEX idx_centros_costo_unidad_id ON centros_costo(unidad_id);
CREATE INDEX idx_movimientos_item_id ON movimientos(item_id);
CREATE INDEX idx_movimientos_centro_costo_id ON movimientos(centro_costo_id);
CREATE INDEX idx_movimientos_anulado_por ON movimientos(anulado_por);
CREATE INDEX idx_movimientos_registrado_por ON movimientos(registrado_por);
CREATE INDEX idx_cierres_mensuales_cerrado_por ON cierres_mensuales(cerrado_por);
CREATE INDEX idx_conceptos_cobro_item_id ON conceptos_cobro(item_id);
CREATE INDEX idx_cargos_beneficiario_id ON cargos(beneficiario_id);
CREATE INDEX idx_cargos_concepto_id ON cargos(concepto_id);
CREATE INDEX idx_cargos_centro_costo_id ON cargos(centro_costo_id);
CREATE INDEX idx_cargos_creado_por ON cargos(creado_por);
CREATE INDEX idx_abonos_cargo_id ON abonos(cargo_id);
CREATE INDEX idx_abonos_movimiento_id ON abonos(movimiento_id);
CREATE INDEX idx_pagos_informados_apoderado_id ON pagos_informados(apoderado_id);
CREATE INDEX idx_pagos_informados_revisado_por ON pagos_informados(revisado_por);
CREATE INDEX idx_movimientos_fecha ON movimientos(fecha);
CREATE INDEX idx_cargos_estado ON cargos(estado);
CREATE INDEX idx_pagos_informados_estado ON pagos_informados(estado);
CREATE INDEX idx_bitacora_entidad ON bitacora(entidad, entidad_id);

-- ===== Datos iniciales =====
INSERT INTO roles (nombre, descripcion) VALUES
  ('administrador','Configura el grupo, usuarios, unidades y voluntarios'),
  ('tesoreria','Registra movimientos, genera cargos, revisa pagos y lleva el inventario'),
  ('dirigente','Gestiona beneficiarios y apoderados de su unidad y registra préstamos'),
  ('apoderado','Usa el portal: estado de cuenta, pagos informados y datos de sus hijos'),
  ('revisor','Comisión revisora: consulta finanzas e inventario en solo lectura');

INSERT INTO items_presupuestarios (codigo, nombre, tipo) VALUES
  ('1.1','Saldo inicial','ingreso'),
  ('2.1','Cuotas de registro institucional','ingreso'),
  ('2.2','Cuotas mensuales de grupo','ingreso'),
  ('3.1','Cuotas de campamento','ingreso'),
  ('3.2','Cuotas de actividades y salidas','ingreso'),
  ('4.1','Venta de indumentaria e insignias','ingreso'),
  ('5.1','Donaciones y aportes','ingreso'),
  ('6.1','Actividades para reunir fondos','ingreso'),
  ('7.1','Pago de registro institucional a la AGSCh','egreso'),
  ('7.2','Gastos de campamentos','egreso'),
  ('7.3','Gastos de actividades y salidas','egreso'),
  ('7.4','Materiales de programa','egreso'),
  ('7.5','Compra de equipamiento e inventario','egreso'),
  ('7.6','Compra de indumentaria e insignias para venta','egreso'),
  ('7.7','Sede, servicios básicos y mantención','egreso'),
  ('7.8','Formación de dirigentes','egreso'),
  ('7.9','Gastos de administración','egreso');
-- Nota: los códigos son una propuesta; se ajustan al formulario DAF-FOR-07 vigente del grupo.

INSERT INTO centros_costo (nombre, tipo) VALUES ('Grupo (gastos generales)','grupo');

COMMIT;
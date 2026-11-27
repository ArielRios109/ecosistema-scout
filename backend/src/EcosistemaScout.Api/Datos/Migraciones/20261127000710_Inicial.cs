using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcosistemaScout.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias_bien",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categorias_bien", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuracion_grupo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre_grupo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    numero_grupo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    distrito = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    zona = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    banco = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    tipo_cuenta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    numero_cuenta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    titular_cuenta = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    rut_titular = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    email_tesoreria = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    logo_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracion_grupo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items_presupuestarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items_presupuestarios", x => x.id);
                    table.CheckConstraint("ck_items_presupuestarios_tipo", "tipo IN ('ingreso', 'egreso')");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    nombre = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unidades",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    rama = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unidades", x => x.id);
                    table.CheckConstraint("ck_unidades_rama", "rama IN ('golondrinas', 'lobatos', 'guias', 'scouts', 'pioneros', 'caminantes')");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    ultimo_acceso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conceptos_cobro",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    monto_sugerido = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conceptos_cobro", x => x.id);
                    table.CheckConstraint("ck_conceptos_cobro_monto_sugerido", "monto_sugerido >= 0");
                    table.CheckConstraint("ck_conceptos_cobro_tipo", "tipo IN ('mensual', 'anual', 'campamento', 'actividad', 'venta', 'otro')");
                    table.ForeignKey(
                        name: "fk_conceptos_cobro_items_presupuestarios_item_id",
                        column: x => x.item_id,
                        principalTable: "items_presupuestarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "rol_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rol_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_rol_claims_roles_role_id",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "centros_costo",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    unidad_id = table.Column<long>(type: "bigint", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_centros_costo", x => x.id);
                    table.CheckConstraint("ck_centros_costo_tipo", "tipo IN ('grupo', 'unidad', 'actividad')");
                    table.ForeignKey(
                        name: "fk_centros_costo_unidades_unidad_id",
                        column: x => x.unidad_id,
                        principalTable: "unidades",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "patrullas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    unidad_id = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patrullas", x => x.id);
                    table.ForeignKey(
                        name: "fk_patrullas_unidades_unidad_id",
                        column: x => x.unidad_id,
                        principalTable: "unidades",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "apoderados",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    rut = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    nombres = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    apellidos = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_apoderados", x => x.id);
                    table.ForeignKey(
                        name: "fk_apoderados_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "bitacora",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    accion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entidad = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entidad_id = table.Column<long>(type: "bigint", nullable: true),
                    antes = table.Column<string>(type: "jsonb", nullable: true),
                    despues = table.Column<string>(type: "jsonb", nullable: true),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bitacora", x => x.id);
                    table.ForeignKey(
                        name: "fk_bitacora_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cierres_mensuales",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    anio = table.Column<short>(type: "smallint", nullable: false),
                    mes = table.Column<short>(type: "smallint", nullable: false),
                    saldo_inicial = table.Column<long>(type: "bigint", nullable: false),
                    total_ingresos = table.Column<long>(type: "bigint", nullable: false),
                    total_egresos = table.Column<long>(type: "bigint", nullable: false),
                    saldo_final = table.Column<long>(type: "bigint", nullable: false),
                    cerrado_por = table.Column<long>(type: "bigint", nullable: false),
                    cerrado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cierres_mensuales", x => x.id);
                    table.CheckConstraint("ck_cierres_mensuales_mes", "mes BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "fk_cierres_mensuales_usuarios_cerrado_por",
                        column: x => x.cerrado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "usuario_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuario_claims_usuarios_user_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_usuario_logins_usuarios_user_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_rol",
                columns: table => new
                {
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    rol_id = table.Column<long>(type: "bigint", nullable: false),
                    unidad_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_rol", x => new { x.usuario_id, x.rol_id });
                    table.ForeignKey(
                        name: "fk_usuario_rol_roles_role_id",
                        column: x => x.rol_id,
                        principalTable: "roles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_usuario_rol_unidades_unidad_id",
                        column: x => x.unidad_id,
                        principalTable: "unidades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_usuario_rol_usuarios_user_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_tokens",
                columns: table => new
                {
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_tokens", x => new { x.usuario_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_usuario_tokens_usuarios_user_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "voluntarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    rut = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    nombres = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    apellidos = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voluntarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_voluntarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "movimientos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    numero_comprobante = table.Column<int>(type: "integer", nullable: false),
                    anio = table.Column<short>(type: "smallint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    centro_costo_id = table.Column<long>(type: "bigint", nullable: false),
                    monto = table.Column<int>(type: "integer", nullable: false),
                    medio_pago = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    tipo_documento = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    numero_documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    respaldo_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    estado = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    motivo_anulacion = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    anulado_por = table.Column<long>(type: "bigint", nullable: true),
                    anulado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    registrado_por = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movimientos", x => x.id);
                    table.CheckConstraint("ck_movimientos_estado", "estado IN ('vigente', 'anulado')");
                    table.CheckConstraint("ck_movimientos_medio_pago", "medio_pago IN ('efectivo', 'transferencia', 'otro')");
                    table.CheckConstraint("ck_movimientos_monto", "monto > 0");
                    table.CheckConstraint("ck_movimientos_tipo", "tipo IN ('ingreso', 'egreso')");
                    table.ForeignKey(
                        name: "fk_movimientos_centros_costo_centro_costo_id",
                        column: x => x.centro_costo_id,
                        principalTable: "centros_costo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_movimientos_items_presupuestarios_item_id",
                        column: x => x.item_id,
                        principalTable: "items_presupuestarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_movimientos_usuarios_anulado_por",
                        column: x => x.anulado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_movimientos_usuarios_registrado_por",
                        column: x => x.registrado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "beneficiarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rut = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    nombres = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    apellidos = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unidad_id = table.Column<long>(type: "bigint", nullable: false),
                    patrulla_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha_ingreso = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    registro_institucional_anio = table.Column<short>(type: "smallint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beneficiarios", x => x.id);
                    table.CheckConstraint("ck_beneficiarios_estado", "estado IN ('activo', 'retirado')");
                    table.ForeignKey(
                        name: "fk_beneficiarios_patrullas_patrulla_id",
                        column: x => x.patrulla_id,
                        principalTable: "patrullas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_beneficiarios_unidades_unidad_id",
                        column: x => x.unidad_id,
                        principalTable: "unidades",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "bienes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    categoria_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    marca = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    numero_serie = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    fecha_adquisicion = table.Column<DateOnly>(type: "date", nullable: true),
                    precio_compra = table.Column<int>(type: "integer", nullable: true),
                    valor_estimado = table.Column<int>(type: "integer", nullable: true),
                    origen = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    ubicacion = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    responsable_id = table.Column<long>(type: "bigint", nullable: true),
                    foto_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    dado_de_baja = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bienes", x => x.id);
                    table.CheckConstraint("ck_bienes_cantidad", "cantidad >= 0");
                    table.CheckConstraint("ck_bienes_estado", "estado IN ('bueno', 'regular', 'malo', 'reparacion')");
                    table.CheckConstraint("ck_bienes_origen", "origen IN ('propio', 'prestamo', 'consignacion', 'comodato', 'donacion')");
                    table.CheckConstraint("ck_bienes_precio_compra", "precio_compra >= 0");
                    table.CheckConstraint("ck_bienes_valor_estimado", "valor_estimado >= 0");
                    table.ForeignKey(
                        name: "fk_bienes_categorias_bien_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias_bien",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_bienes_voluntarios_responsable_id",
                        column: x => x.responsable_id,
                        principalTable: "voluntarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cargos_voluntario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    voluntario_id = table.Column<long>(type: "bigint", nullable: false),
                    unidad_id = table.Column<long>(type: "bigint", nullable: true),
                    cargo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_termino = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cargos_voluntario", x => x.id);
                    table.ForeignKey(
                        name: "fk_cargos_voluntario_unidades_unidad_id",
                        column: x => x.unidad_id,
                        principalTable: "unidades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_cargos_voluntario_voluntarios_voluntario_id",
                        column: x => x.voluntario_id,
                        principalTable: "voluntarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pagos_informados",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    apoderado_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha_transferencia = table.Column<DateOnly>(type: "date", nullable: false),
                    monto = table.Column<int>(type: "integer", nullable: false),
                    banco_origen = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    numero_operacion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    comprobante_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    estado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    motivo_rechazo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    revisado_por = table.Column<long>(type: "bigint", nullable: true),
                    revisado_en = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    movimiento_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pagos_informados", x => x.id);
                    table.CheckConstraint("ck_pagos_informados_estado", "estado IN ('pendiente', 'aprobado', 'rechazado')");
                    table.CheckConstraint("ck_pagos_informados_monto", "monto > 0");
                    table.ForeignKey(
                        name: "fk_pagos_informados_apoderados_apoderado_id",
                        column: x => x.apoderado_id,
                        principalTable: "apoderados",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pagos_informados_movimientos_movimiento_id",
                        column: x => x.movimiento_id,
                        principalTable: "movimientos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pagos_informados_usuarios_revisado_por",
                        column: x => x.revisado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "beneficiario_apoderado",
                columns: table => new
                {
                    beneficiario_id = table.Column<long>(type: "bigint", nullable: false),
                    apoderado_id = table.Column<long>(type: "bigint", nullable: false),
                    parentesco = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beneficiario_apoderado", x => new { x.beneficiario_id, x.apoderado_id });
                    table.ForeignKey(
                        name: "fk_beneficiario_apoderado_apoderados_apoderado_id",
                        column: x => x.apoderado_id,
                        principalTable: "apoderados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_beneficiario_apoderado_beneficiarios_beneficiario_id",
                        column: x => x.beneficiario_id,
                        principalTable: "beneficiarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cargos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    beneficiario_id = table.Column<long>(type: "bigint", nullable: false),
                    concepto_id = table.Column<long>(type: "bigint", nullable: false),
                    centro_costo_id = table.Column<long>(type: "bigint", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    monto = table.Column<int>(type: "integer", nullable: false),
                    fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    creado_por = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cargos", x => x.id);
                    table.CheckConstraint("ck_cargos_estado", "estado IN ('pendiente', 'parcial', 'pagado', 'anulado')");
                    table.CheckConstraint("ck_cargos_monto", "monto > 0");
                    table.ForeignKey(
                        name: "fk_cargos_beneficiarios_beneficiario_id",
                        column: x => x.beneficiario_id,
                        principalTable: "beneficiarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_cargos_centros_costo_centro_costo_id",
                        column: x => x.centro_costo_id,
                        principalTable: "centros_costo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_cargos_conceptos_cobro_concepto_id",
                        column: x => x.concepto_id,
                        principalTable: "conceptos_cobro",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_cargos_usuarios_creado_por",
                        column: x => x.creado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contactos_emergencia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    beneficiario_id = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    parentesco = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contactos_emergencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_contactos_emergencia_beneficiarios_beneficiario_id",
                        column: x => x.beneficiario_id,
                        principalTable: "beneficiarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fichas_medicas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    beneficiario_id = table.Column<long>(type: "bigint", nullable: false),
                    prevision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    grupo_sanguineo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    alergias = table.Column<string>(type: "text", nullable: true),
                    enfermedades = table.Column<string>(type: "text", nullable: true),
                    medicamentos = table.Column<string>(type: "text", nullable: true),
                    restricciones_alimentarias = table.Column<string>(type: "text", nullable: true),
                    necesidades_emocionales = table.Column<string>(type: "text", nullable: true),
                    actualizado_por = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fichas_medicas", x => x.id);
                    table.ForeignKey(
                        name: "fk_fichas_medicas_beneficiarios_beneficiario_id",
                        column: x => x.beneficiario_id,
                        principalTable: "beneficiarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fichas_medicas_usuarios_actualizado_por",
                        column: x => x.actualizado_por,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historial_beneficiario",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    beneficiario_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    unidad_origen_id = table.Column<long>(type: "bigint", nullable: true),
                    unidad_destino_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    detalle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    usuario_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historial_beneficiario", x => x.id);
                    table.CheckConstraint("ck_historial_beneficiario_tipo", "tipo IN ('ingreso', 'cambio_unidad', 'cambio_patrulla', 'retiro', 'reingreso')");
                    table.ForeignKey(
                        name: "fk_historial_beneficiario_beneficiarios_beneficiario_id",
                        column: x => x.beneficiario_id,
                        principalTable: "beneficiarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_historial_beneficiario_unidades_unidad_destino_id",
                        column: x => x.unidad_destino_id,
                        principalTable: "unidades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_historial_beneficiario_unidades_unidad_origen_id",
                        column: x => x.unidad_origen_id,
                        principalTable: "unidades",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_historial_beneficiario_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "bajas_bien",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bien_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    detalle = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    registrado_por = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bajas_bien", x => x.id);
                    table.CheckConstraint("ck_bajas_bien_motivo", "motivo IN ('venta', 'robo', 'donacion', 'deterioro', 'perdida', 'otro')");
                    table.ForeignKey(
                        name: "fk_bajas_bien_bienes_bien_id",
                        column: x => x.bien_id,
                        principalTable: "bienes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_bajas_bien_usuarios_registrado_por",
                        column: x => x.registrado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "prestamos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bien_id = table.Column<long>(type: "bigint", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    destinatario = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    actividad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    fecha_salida = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_devolucion_esperada = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_devolucion = table.Column<DateOnly>(type: "date", nullable: true),
                    estado_devolucion = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    registrado_por = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prestamos", x => x.id);
                    table.CheckConstraint("ck_prestamos_cantidad", "cantidad > 0");
                    table.ForeignKey(
                        name: "fk_prestamos_bienes_bien_id",
                        column: x => x.bien_id,
                        principalTable: "bienes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_prestamos_usuarios_registrado_por",
                        column: x => x.registrado_por,
                        principalTable: "usuarios",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "abonos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cargo_id = table.Column<long>(type: "bigint", nullable: false),
                    movimiento_id = table.Column<long>(type: "bigint", nullable: false),
                    monto = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_abonos", x => x.id);
                    table.CheckConstraint("ck_abonos_monto", "monto > 0");
                    table.ForeignKey(
                        name: "fk_abonos_cargos_cargo_id",
                        column: x => x.cargo_id,
                        principalTable: "cargos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_abonos_movimientos_movimiento_id",
                        column: x => x.movimiento_id,
                        principalTable: "movimientos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "pago_informado_cargo",
                columns: table => new
                {
                    pago_informado_id = table.Column<long>(type: "bigint", nullable: false),
                    cargo_id = table.Column<long>(type: "bigint", nullable: false),
                    monto_aplicado = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pago_informado_cargo", x => new { x.pago_informado_id, x.cargo_id });
                    table.CheckConstraint("ck_pago_informado_cargo_monto_aplicado", "monto_aplicado > 0");
                    table.ForeignKey(
                        name: "fk_pago_informado_cargo_cargos_cargo_id",
                        column: x => x.cargo_id,
                        principalTable: "cargos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pago_informado_cargo_pagos_informados_pago_informado_id",
                        column: x => x.pago_informado_id,
                        principalTable: "pagos_informados",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_abonos_cargo_id",
                table: "abonos",
                column: "cargo_id");

            migrationBuilder.CreateIndex(
                name: "ix_abonos_movimiento_id",
                table: "abonos",
                column: "movimiento_id");

            migrationBuilder.CreateIndex(
                name: "ix_apoderados_rut",
                table: "apoderados",
                column: "rut",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_apoderados_usuario_id",
                table: "apoderados",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bajas_bien_bien_id",
                table: "bajas_bien",
                column: "bien_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bajas_bien_registrado_por",
                table: "bajas_bien",
                column: "registrado_por");

            migrationBuilder.CreateIndex(
                name: "ix_beneficiario_apoderado_apoderado_id",
                table: "beneficiario_apoderado",
                column: "apoderado_id");

            migrationBuilder.CreateIndex(
                name: "ix_beneficiarios_patrulla_id",
                table: "beneficiarios",
                column: "patrulla_id");

            migrationBuilder.CreateIndex(
                name: "ix_beneficiarios_rut",
                table: "beneficiarios",
                column: "rut",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_beneficiarios_unidad_id",
                table: "beneficiarios",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_categoria_id",
                table: "bienes",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_bienes_codigo",
                table: "bienes",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bienes_responsable_id",
                table: "bienes",
                column: "responsable_id");

            migrationBuilder.CreateIndex(
                name: "ix_bitacora_entidad_entidad_id",
                table: "bitacora",
                columns: new[] { "entidad", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "ix_bitacora_usuario_id",
                table: "bitacora",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_cargos_beneficiario_id",
                table: "cargos",
                column: "beneficiario_id");

            migrationBuilder.CreateIndex(
                name: "ix_cargos_centro_costo_id",
                table: "cargos",
                column: "centro_costo_id");

            migrationBuilder.CreateIndex(
                name: "ix_cargos_concepto_id",
                table: "cargos",
                column: "concepto_id");

            migrationBuilder.CreateIndex(
                name: "ix_cargos_creado_por",
                table: "cargos",
                column: "creado_por");

            migrationBuilder.CreateIndex(
                name: "ix_cargos_estado",
                table: "cargos",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_cargos_voluntario_unidad_id",
                table: "cargos_voluntario",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_cargos_voluntario_voluntario_id",
                table: "cargos_voluntario",
                column: "voluntario_id");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_bien_nombre",
                table: "categorias_bien",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_centros_costo_unidad_id",
                table: "centros_costo",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "ix_cierres_mensuales_anio_mes",
                table: "cierres_mensuales",
                columns: new[] { "anio", "mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cierres_mensuales_cerrado_por",
                table: "cierres_mensuales",
                column: "cerrado_por");

            migrationBuilder.CreateIndex(
                name: "ix_conceptos_cobro_item_id",
                table: "conceptos_cobro",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_contactos_emergencia_beneficiario_id",
                table: "contactos_emergencia",
                column: "beneficiario_id");

            migrationBuilder.CreateIndex(
                name: "ix_fichas_medicas_actualizado_por",
                table: "fichas_medicas",
                column: "actualizado_por");

            migrationBuilder.CreateIndex(
                name: "ix_fichas_medicas_beneficiario_id",
                table: "fichas_medicas",
                column: "beneficiario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_historial_beneficiario_beneficiario_id",
                table: "historial_beneficiario",
                column: "beneficiario_id");

            migrationBuilder.CreateIndex(
                name: "ix_historial_beneficiario_unidad_destino_id",
                table: "historial_beneficiario",
                column: "unidad_destino_id");

            migrationBuilder.CreateIndex(
                name: "ix_historial_beneficiario_unidad_origen_id",
                table: "historial_beneficiario",
                column: "unidad_origen_id");

            migrationBuilder.CreateIndex(
                name: "ix_historial_beneficiario_usuario_id",
                table: "historial_beneficiario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_presupuestarios_codigo",
                table: "items_presupuestarios",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_anulado_por",
                table: "movimientos",
                column: "anulado_por");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_centro_costo_id",
                table: "movimientos",
                column: "centro_costo_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_fecha",
                table: "movimientos",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_item_id",
                table: "movimientos",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_registrado_por",
                table: "movimientos",
                column: "registrado_por");

            migrationBuilder.CreateIndex(
                name: "ix_movimientos_tipo_anio_numero_comprobante",
                table: "movimientos",
                columns: new[] { "tipo", "anio", "numero_comprobante" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pago_informado_cargo_cargo_id",
                table: "pago_informado_cargo",
                column: "cargo_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_informados_apoderado_id",
                table: "pagos_informados",
                column: "apoderado_id");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_informados_banco_origen_numero_operacion",
                table: "pagos_informados",
                columns: new[] { "banco_origen", "numero_operacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pagos_informados_estado",
                table: "pagos_informados",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_pagos_informados_movimiento_id",
                table: "pagos_informados",
                column: "movimiento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pagos_informados_revisado_por",
                table: "pagos_informados",
                column: "revisado_por");

            migrationBuilder.CreateIndex(
                name: "ix_patrullas_unidad_id_nombre",
                table: "patrullas",
                columns: new[] { "unidad_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prestamos_bien_id",
                table: "prestamos",
                column: "bien_id");

            migrationBuilder.CreateIndex(
                name: "ix_prestamos_registrado_por",
                table: "prestamos",
                column: "registrado_por");

            migrationBuilder.CreateIndex(
                name: "ix_rol_claims_role_id",
                table: "rol_claims",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_claims_user_id",
                table: "usuario_claims",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_logins_user_id",
                table: "usuario_logins",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_rol_role_id",
                table: "usuario_rol",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuario_rol_unidad_id",
                table: "usuario_rol",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "usuarios",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "usuarios",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_voluntarios_rut",
                table: "voluntarios",
                column: "rut",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_voluntarios_usuario_id",
                table: "voluntarios",
                column: "usuario_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abonos");

            migrationBuilder.DropTable(
                name: "bajas_bien");

            migrationBuilder.DropTable(
                name: "beneficiario_apoderado");

            migrationBuilder.DropTable(
                name: "bitacora");

            migrationBuilder.DropTable(
                name: "cargos_voluntario");

            migrationBuilder.DropTable(
                name: "cierres_mensuales");

            migrationBuilder.DropTable(
                name: "configuracion_grupo");

            migrationBuilder.DropTable(
                name: "contactos_emergencia");

            migrationBuilder.DropTable(
                name: "fichas_medicas");

            migrationBuilder.DropTable(
                name: "historial_beneficiario");

            migrationBuilder.DropTable(
                name: "pago_informado_cargo");

            migrationBuilder.DropTable(
                name: "prestamos");

            migrationBuilder.DropTable(
                name: "rol_claims");

            migrationBuilder.DropTable(
                name: "usuario_claims");

            migrationBuilder.DropTable(
                name: "usuario_logins");

            migrationBuilder.DropTable(
                name: "usuario_rol");

            migrationBuilder.DropTable(
                name: "usuario_tokens");

            migrationBuilder.DropTable(
                name: "cargos");

            migrationBuilder.DropTable(
                name: "pagos_informados");

            migrationBuilder.DropTable(
                name: "bienes");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "beneficiarios");

            migrationBuilder.DropTable(
                name: "conceptos_cobro");

            migrationBuilder.DropTable(
                name: "apoderados");

            migrationBuilder.DropTable(
                name: "movimientos");

            migrationBuilder.DropTable(
                name: "categorias_bien");

            migrationBuilder.DropTable(
                name: "voluntarios");

            migrationBuilder.DropTable(
                name: "patrullas");

            migrationBuilder.DropTable(
                name: "centros_costo");

            migrationBuilder.DropTable(
                name: "items_presupuestarios");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "unidades");
        }
    }
}

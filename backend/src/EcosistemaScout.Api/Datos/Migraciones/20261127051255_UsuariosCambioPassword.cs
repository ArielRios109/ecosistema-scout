using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcosistemaScout.Api.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class UsuariosCambioPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "debe_cambiar_password",
                table: "usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "debe_cambiar_password",
                table: "usuarios");
        }
    }
}

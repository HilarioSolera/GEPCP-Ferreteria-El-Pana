using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposEmpleadoMas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Direccion",
                table: "Empleados",
                newName: "DireccionExacta");

            migrationBuilder.AddColumn<string>(
                name: "DireccionCanton",
                table: "Empleados",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionDistrito",
                table: "Empleados",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionProvincia",
                table: "Empleados",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DireccionCanton",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "DireccionDistrito",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "DireccionProvincia",
                table: "Empleados");

            migrationBuilder.RenameColumn(
                name: "DireccionExacta",
                table: "Empleados",
                newName: "Direccion");
        }
    }
}

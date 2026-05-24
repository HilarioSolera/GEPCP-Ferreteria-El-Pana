using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AddBancoAndRelacionContactoToEmpleado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Banco",
                table: "Empleados",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelacionContactoEmergencia",
                table: "Empleados",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 1,
                column: "Departamento",
                value: "Recursos Humanos");

            migrationBuilder.UpdateData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 2,
                column: "Departamento",
                value: "Ventas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banco",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "RelacionContactoEmergencia",
                table: "Empleados");

            migrationBuilder.UpdateData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 1,
                column: "Departamento",
                value: "");

            migrationBuilder.UpdateData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 2,
                column: "Departamento",
                value: "");
        }
    }
}

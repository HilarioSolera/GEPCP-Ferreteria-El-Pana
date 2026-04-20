using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDepartamentoCodigoAPuesto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Puestos",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Puestos",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 1,
                columns: new[] { "Codigo", "Departamento" },
                values: new object[] { "TOCG", "" });

            migrationBuilder.UpdateData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 2,
                columns: new[] { "Codigo", "Departamento" },
                values: new object[] { "TOCG", "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Puestos");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Puestos");
        }
    }
}

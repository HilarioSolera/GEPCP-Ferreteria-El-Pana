using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTotalDiasIncapacidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 14);

            migrationBuilder.AddColumn<int>(
                name: "TotalDias",
                table: "Incapacidades",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalDias",
                table: "Incapacidades");

            migrationBuilder.InsertData(
                table: "Puestos",
                columns: new[] { "PuestoId", "Activo", "Nombre", "SalarioBase" },
                values: new object[,]
                {
                    { 10, true, "Conductor (TOSC)", 436585m },
                    { 11, true, "Bodeguero (TONCG)", 447778m },
                    { 12, true, "Cajero (TOCG)", 447778m },
                    { 13, true, "Asistente (TOCG)", 410855m },
                    { 14, true, "Proveedor (TOCG)", 492556m }
                });
        }
    }
}

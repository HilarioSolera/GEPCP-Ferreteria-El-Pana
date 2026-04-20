using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class SeedPuestosEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Puestos",
                columns: new[] { "PuestoId", "Activo", "Codigo", "Departamento", "Nombre", "SalarioBase" },
                values: new object[,]
                {
                    { 100, true, "TOCG", "Administrativo", "Asistente", 410855.00m },
                    { 101, true, "TOCG", "Administrativo", "Proveeduría", 492556.00m },
                    { 102, true, "TOCG", "Caja", "Cajero", 477778.00m },
                    { 103, true, "TOCG", "Ventas", "Demostrador-vendedor", 447778.00m },
                    { 104, true, "TOCG", "Bodega", "Bodeguero", 447778.00m },
                    { 105, true, "TOCG", "Conductores", "Conductor", 436585.00m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 105);
        }
    }
}

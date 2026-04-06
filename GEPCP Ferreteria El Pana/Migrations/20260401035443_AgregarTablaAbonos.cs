using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablaAbonos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbonoPrestamo_Prestamos_PrestamoId",
                table: "AbonoPrestamo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbonoPrestamo",
                table: "AbonoPrestamo");

            migrationBuilder.RenameTable(
                name: "AbonoPrestamo",
                newName: "AbonosPrestamo");

            migrationBuilder.RenameIndex(
                name: "IX_AbonoPrestamo_PrestamoId",
                table: "AbonosPrestamo",
                newName: "IX_AbonosPrestamo_PrestamoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbonosPrestamo",
                table: "AbonosPrestamo",
                column: "AbonoPrestamoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AbonosPrestamo_Prestamos_PrestamoId",
                table: "AbonosPrestamo",
                column: "PrestamoId",
                principalTable: "Prestamos",
                principalColumn: "PrestamoId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbonosPrestamo_Prestamos_PrestamoId",
                table: "AbonosPrestamo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbonosPrestamo",
                table: "AbonosPrestamo");

            migrationBuilder.RenameTable(
                name: "AbonosPrestamo",
                newName: "AbonoPrestamo");

            migrationBuilder.RenameIndex(
                name: "IX_AbonosPrestamo_PrestamoId",
                table: "AbonoPrestamo",
                newName: "IX_AbonoPrestamo_PrestamoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbonoPrestamo",
                table: "AbonoPrestamo",
                column: "AbonoPrestamoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AbonoPrestamo_Prestamos_PrestamoId",
                table: "AbonoPrestamo",
                column: "PrestamoId",
                principalTable: "Prestamos",
                principalColumn: "PrestamoId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

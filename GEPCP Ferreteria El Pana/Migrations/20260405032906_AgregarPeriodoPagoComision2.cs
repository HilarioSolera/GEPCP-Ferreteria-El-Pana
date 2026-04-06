using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPeriodoPagoComision2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeriodoPagoId",
                table: "Comisiones",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comisiones_PeriodoPagoId",
                table: "Comisiones",
                column: "PeriodoPagoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comisiones_PeriodosPago_PeriodoPagoId",
                table: "Comisiones",
                column: "PeriodoPagoId",
                principalTable: "PeriodosPago",
                principalColumn: "PeriodoPagoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comisiones_PeriodosPago_PeriodoPagoId",
                table: "Comisiones");

            migrationBuilder.DropIndex(
                name: "IX_Comisiones_PeriodoPagoId",
                table: "Comisiones");

            migrationBuilder.DropColumn(
                name: "PeriodoPagoId",
                table: "Comisiones");
        }
    }
}

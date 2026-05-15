using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCalculadoraManualAguinaldo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoEspecieMensual",
                table: "Aguinaldos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeEspecie",
                table: "Aguinaldos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SalariosMensuales",
                table: "Aguinaldos",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoEspecieMensual",
                table: "Aguinaldos");

            migrationBuilder.DropColumn(
                name: "PorcentajeEspecie",
                table: "Aguinaldos");

            migrationBuilder.DropColumn(
                name: "SalariosMensuales",
                table: "Aguinaldos");
        }
    }
}

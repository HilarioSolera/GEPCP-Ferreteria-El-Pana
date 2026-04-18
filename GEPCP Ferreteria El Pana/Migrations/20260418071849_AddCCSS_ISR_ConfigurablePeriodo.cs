using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AddCCSS_ISR_ConfigurablePeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ISR_CreditoConyuge",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_CreditoHijo",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo1_Hasta",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo2_Desde",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo2_Hasta",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo2_Porcentaje",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo3_Desde",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo3_Hasta",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo3_Porcentaje",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo4_Desde",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo4_Hasta",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo4_Porcentaje",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo5_Desde",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ISR_Tramo5_Porcentaje",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeBP",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeIVM",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeSEM",
                table: "PeriodosPago",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ISR_CreditoConyuge",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_CreditoHijo",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo1_Hasta",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo2_Desde",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo2_Hasta",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo2_Porcentaje",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo3_Desde",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo3_Hasta",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo3_Porcentaje",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo4_Desde",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo4_Hasta",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo4_Porcentaje",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo5_Desde",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "ISR_Tramo5_Porcentaje",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "PorcentajeBP",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "PorcentajeIVM",
                table: "PeriodosPago");

            migrationBuilder.DropColumn(
                name: "PorcentajeSEM",
                table: "PeriodosPago");
        }
    }
}

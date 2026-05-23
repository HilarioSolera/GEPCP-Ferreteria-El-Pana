using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMontoVacacionesIncapacidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoIncapacidades",
                table: "PlanillasEmpleado",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoVacaciones",
                table: "PlanillasEmpleado",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoIncapacidades",
                table: "PlanillasEmpleado");

            migrationBuilder.DropColumn(
                name: "MontoVacaciones",
                table: "PlanillasEmpleado");
        }
    }
}

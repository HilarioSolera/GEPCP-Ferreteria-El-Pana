using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTrabajadoPagoFeriado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Trabajado",
                table: "PagosFeriado",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Trabajado",
                table: "PagosFeriado");
        }
    }
}

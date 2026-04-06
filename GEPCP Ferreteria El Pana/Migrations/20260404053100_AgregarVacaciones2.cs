using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVacaciones2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vacaciones",
                columns: table => new
                {
                    VacacionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    DiasHabiles = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiasDisponiblesAlRegistrar = table.Column<decimal>(type: "TEXT", nullable: false),
                    MontoDeducido = table.Column<decimal>(type: "TEXT", nullable: false),
                    SalarioDiario = table.Column<decimal>(type: "TEXT", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RegistradoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacaciones", x => x.VacacionId);
                    table.ForeignKey(
                        name: "FK_Vacaciones_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacaciones_EmpleadoId",
                table: "Vacaciones",
                column: "EmpleadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vacaciones");
        }
    }
}

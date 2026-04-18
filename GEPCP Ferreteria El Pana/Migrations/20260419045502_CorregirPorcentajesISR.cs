using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class CorregirPorcentajesISR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Corregir períodos con porcentajes ISR en 0 (bug: migración anterior
            // agregó columnas con defaultValue = 0 en lugar de los valores reales)
            migrationBuilder.Sql(@"
                UPDATE PeriodosPago SET
                    ISR_Tramo2_Porcentaje = 10,
                    ISR_Tramo3_Porcentaje = 15,
                    ISR_Tramo4_Porcentaje = 20,
                    ISR_Tramo5_Porcentaje = 25
                WHERE ISR_Tramo2_Porcentaje = 0
                   AND ISR_Tramo3_Porcentaje = 0
                   AND ISR_Tramo4_Porcentaje = 0
                   AND ISR_Tramo5_Porcentaje = 0
            ");

            // Corregir montos 'Desde' que estén en 0
            migrationBuilder.Sql(@"
                UPDATE PeriodosPago SET
                    ISR_Tramo2_Desde = 918000,
                    ISR_Tramo3_Desde = 1347000,
                    ISR_Tramo4_Desde = 2364000,
                    ISR_Tramo5_Desde = 4727000
                WHERE ISR_Tramo2_Desde = 0
                   AND ISR_Tramo3_Desde = 0
                   AND ISR_Tramo4_Desde = 0
                   AND ISR_Tramo5_Desde = 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarTramosISR2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Actualizar tramos ISR a valores 2026 en todos los períodos existentes
            migrationBuilder.Sql(@"
                UPDATE PeriodosPago SET
                    ISR_Tramo1_Hasta     = 918000,
                    ISR_Tramo2_Desde     = 918000,
                    ISR_Tramo2_Hasta     = 1347000,
                    ISR_Tramo3_Desde     = 1347000,
                    ISR_Tramo3_Hasta     = 2364000,
                    ISR_Tramo4_Desde     = 2364000,
                    ISR_Tramo4_Hasta     = 4727000,
                    ISR_Tramo5_Desde     = 4727000
                WHERE ISR_Tramo1_Hasta = 929000
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir a valores anteriores
            migrationBuilder.Sql(@"
                UPDATE PeriodosPago SET
                    ISR_Tramo1_Hasta     = 929000,
                    ISR_Tramo2_Desde     = 929000,
                    ISR_Tramo2_Hasta     = 1363000,
                    ISR_Tramo3_Desde     = 1363000,
                    ISR_Tramo3_Hasta     = 2392000,
                    ISR_Tramo4_Desde     = 2392000,
                    ISR_Tramo4_Hasta     = 4783000,
                    ISR_Tramo5_Desde     = 4783000
                WHERE ISR_Tramo1_Hasta = 918000
            ");
        }
    }
}

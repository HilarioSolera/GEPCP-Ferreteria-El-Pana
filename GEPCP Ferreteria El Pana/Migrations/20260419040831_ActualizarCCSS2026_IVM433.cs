using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarCCSS2026_IVM433 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Actualizar CCSS a valores 2026: IVM 4.33%, total 10.83%
            migrationBuilder.Sql(@"
                UPDATE PeriodosPago SET
                    PorcentajeIVM  = 4.33,
                    PorcentajeCCSS = 10.83
                WHERE PorcentajeIVM = 4.17 AND PorcentajeCCSS = 10.67
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE PeriodosPago SET
                    PorcentajeIVM  = 4.17,
                    PorcentajeCCSS = 10.67
                WHERE PorcentajeIVM = 4.33 AND PorcentajeCCSS = 10.83
            ");
        }
    }
}

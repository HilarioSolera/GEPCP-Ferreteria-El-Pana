using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class CorregirDbSetAbonosCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbonoCreditoFerreteria_CreditosFerreteria_CreditoFerreteriaId",
                table: "AbonoCreditoFerreteria");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbonoCreditoFerreteria",
                table: "AbonoCreditoFerreteria");

            migrationBuilder.RenameTable(
                name: "AbonoCreditoFerreteria",
                newName: "AbonosCreditoFerreteria");

            migrationBuilder.RenameIndex(
                name: "IX_AbonoCreditoFerreteria_CreditoFerreteriaId",
                table: "AbonosCreditoFerreteria",
                newName: "IX_AbonosCreditoFerreteria_CreditoFerreteriaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbonosCreditoFerreteria",
                table: "AbonosCreditoFerreteria",
                column: "AbonoCreditoFerreteriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AbonosCreditoFerreteria_CreditosFerreteria_CreditoFerreteriaId",
                table: "AbonosCreditoFerreteria",
                column: "CreditoFerreteriaId",
                principalTable: "CreditosFerreteria",
                principalColumn: "CreditoFerreteriaId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbonosCreditoFerreteria_CreditosFerreteria_CreditoFerreteriaId",
                table: "AbonosCreditoFerreteria");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbonosCreditoFerreteria",
                table: "AbonosCreditoFerreteria");

            migrationBuilder.RenameTable(
                name: "AbonosCreditoFerreteria",
                newName: "AbonoCreditoFerreteria");

            migrationBuilder.RenameIndex(
                name: "IX_AbonosCreditoFerreteria_CreditoFerreteriaId",
                table: "AbonoCreditoFerreteria",
                newName: "IX_AbonoCreditoFerreteria_CreditoFerreteriaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbonoCreditoFerreteria",
                table: "AbonoCreditoFerreteria",
                column: "AbonoCreditoFerreteriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_AbonoCreditoFerreteria_CreditosFerreteria_CreditoFerreteriaId",
                table: "AbonoCreditoFerreteria",
                column: "CreditoFerreteriaId",
                principalTable: "CreditosFerreteria",
                principalColumn: "CreditoFerreteriaId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

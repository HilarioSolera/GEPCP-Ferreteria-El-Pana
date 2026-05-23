using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class RegenerarUsuariosSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "UsuarioId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$y6tuNj6/4sGOWx2WhAP9IO4vqO9P1kHIWNZUYdi1yLM1VagXKIlbS");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "UsuarioId",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$QwnWnzunvEvMfARNsI5xmuvqqiymZKd3TqVT1QsXCEQjODq5htYhG");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "UsuarioId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$/mJGbQrxHo3bDUtdY6MWoeaJc/6aYPE7EG9ukr6ln9mNupX3Y8Wz.");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "UsuarioId",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$T72F0Mu8ocYejSTck6bprueMSoi5WgVtSD.hIraw5PvhnjDde6rD6");
        }
    }
}

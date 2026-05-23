using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class EliminarUsuariosSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "UsuarioId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "UsuarioId",
                keyValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "UsuarioId", "CorreoElectronico", "NombreCompleto", "NombreUsuario", "PasswordHash", "Rol", "TokenExpiracion", "TokenRecuperacion" },
                values: new object[,]
                {
                    { 1, "solerahilario207@gmail.com", "Administrador RRHH", "admin.rrhh", "$2a$11$y6tuNj6/4sGOWx2WhAP9IO4vqO9P1kHIWNZUYdi1yLM1VagXKIlbS", "RRHH", null, null },
                    { 2, "solerahilario207@gmail.com", "Usuario Jefatura", "jefatura", "$2a$11$QwnWnzunvEvMfARNsI5xmuvqqiymZKd3TqVT1QsXCEQjODq5htYhG", "Jefatura", null, null }
                });
        }
    }
}

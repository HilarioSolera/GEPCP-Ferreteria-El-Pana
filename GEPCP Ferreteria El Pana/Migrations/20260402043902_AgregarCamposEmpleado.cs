using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposEmpleado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactoEmergenciaNombre",
                table: "Empleados",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactoEmergenciaTelefono",
                table: "Empleados",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Empleados",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimientoContrato",
                table: "Empleados",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoContrato",
                table: "Empleados",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactoEmergenciaNombre",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "ContactoEmergenciaTelefono",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "FechaVencimientoContrato",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Empleados");
        }
    }
}

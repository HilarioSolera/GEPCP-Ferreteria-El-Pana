using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class RedisenoCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "Empleados",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaIngreso",
                table: "Empleados",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TipoJornada",
                table: "Empleados",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CreditosFerreteria",
                columns: table => new
                {
                    CreditoFerreteriaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Saldo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CuotaQuincenal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCredito = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditosFerreteria", x => x.CreditoFerreteriaId);
                    table.ForeignKey(
                        name: "FK_CreditosFerreteria_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    FeriadoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.FeriadoId);
                });

            migrationBuilder.CreateTable(
                name: "Incapacidades",
                columns: table => new
                {
                    IncapacidadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Entidad = table.Column<int>(type: "int", nullable: false),
                    TipoIncapacidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TiqueteCCSS = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PorcentajePago = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ResponsablePago = table.Column<int>(type: "int", nullable: false),
                    MontoPorDia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incapacidades", x => x.IncapacidadId);
                    table.ForeignKey(
                        name: "FK_Incapacidades_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosPago",
                columns: table => new
                {
                    PeriodoPagoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quincena = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosPago", x => x.PeriodoPagoId);
                });

            migrationBuilder.CreateTable(
                name: "HorasExtras",
                columns: table => new
                {
                    HorasExtrasId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    PeriodoPagoId = table.Column<int>(type: "int", nullable: false),
                    TotalHoras = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ValorHora = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Porcentaje = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorasExtras", x => x.HorasExtrasId);
                    table.ForeignKey(
                        name: "FK_HorasExtras_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HorasExtras_PeriodosPago_PeriodoPagoId",
                        column: x => x.PeriodoPagoId,
                        principalTable: "PeriodosPago",
                        principalColumn: "PeriodoPagoId");
                });

            migrationBuilder.CreateTable(
                name: "PagosFeriado",
                columns: table => new
                {
                    PagoFeriadoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    FeriadoId = table.Column<int>(type: "int", nullable: false),
                    PeriodoPagoId = table.Column<int>(type: "int", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosFeriado", x => x.PagoFeriadoId);
                    table.ForeignKey(
                        name: "FK_PagosFeriado_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagosFeriado_Feriados_FeriadoId",
                        column: x => x.FeriadoId,
                        principalTable: "Feriados",
                        principalColumn: "FeriadoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PagosFeriado_PeriodosPago_PeriodoPagoId",
                        column: x => x.PeriodoPagoId,
                        principalTable: "PeriodosPago",
                        principalColumn: "PeriodoPagoId");
                });

            migrationBuilder.CreateTable(
                name: "PlanillasEmpleado",
                columns: table => new
                {
                    PlanillaEmpleadoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodoPagoId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    HorasOrdinarias = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HorasExtras = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HorasNoLaboradas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorHora = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorHoraExtra = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalarioOrdinario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AumentoAplicado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoHorasExtras = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoFeriados = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDevengado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PorcentajeCCSS = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DeduccionCCSS = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeduccionPrestamos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeduccionCreditoFerreteria = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeduccionIncapacidad = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeduccionHorasNoLaboradas = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtrasDeducciones = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeducciones = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetoAPagar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanillasEmpleado", x => x.PlanillaEmpleadoId);
                    table.ForeignKey(
                        name: "FK_PlanillasEmpleado_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanillasEmpleado_PeriodosPago_PeriodoPagoId",
                        column: x => x.PeriodoPagoId,
                        principalTable: "PeriodosPago",
                        principalColumn: "PeriodoPagoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "FeriadoId", "Fecha", "Nombre", "Tipo" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Año Nuevo", 1 },
                    { 2, new DateTime(2026, 4, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Jueves Santo", 1 },
                    { 3, new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Viernes Santo", 1 },
                    { 4, new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia de Juan Santamaria", 2 },
                    { 5, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia del Trabajador", 1 },
                    { 6, new DateTime(2026, 7, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Anexion Guanacaste", 2 },
                    { 7, new DateTime(2026, 8, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Virgen de los Angeles", 2 },
                    { 8, new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia de la Madre", 2 },
                    { 9, new DateTime(2026, 9, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia de la Independencia", 1 },
                    { 10, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Navidad", 1 }
                });

            migrationBuilder.InsertData(
     table: "Puestos",
     columns: new[] { "PuestoId", "Activo", "Nombre", "SalarioBase" },
     values: new object[,]
     {
        { 10, true, "Conductor (TOSC)", 436585.0m },
        { 11, true, "Bodeguero (TONCG)", 447778.0m },
        { 12, true, "Cajero (TOCG)", 447778.0m },
        { 13, true, "Asistente (TOCG)", 410855.0m },
        { 14, true, "Proveedor (TOCG)", 492556.0m }
     });

            migrationBuilder.CreateIndex(
                name: "IX_CreditosFerreteria_EmpleadoId",
                table: "CreditosFerreteria",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_HorasExtras_EmpleadoId",
                table: "HorasExtras",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_HorasExtras_PeriodoPagoId",
                table: "HorasExtras",
                column: "PeriodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_Incapacidades_EmpleadoId",
                table: "Incapacidades",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosFeriado_EmpleadoId",
                table: "PagosFeriado",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosFeriado_FeriadoId",
                table: "PagosFeriado",
                column: "FeriadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PagosFeriado_PeriodoPagoId",
                table: "PagosFeriado",
                column: "PeriodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanillasEmpleado_EmpleadoId_PeriodoPagoId",
                table: "PlanillasEmpleado",
                columns: new[] { "EmpleadoId", "PeriodoPagoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanillasEmpleado_PeriodoPagoId",
                table: "PlanillasEmpleado",
                column: "PeriodoPagoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditosFerreteria");

            migrationBuilder.DropTable(
                name: "HorasExtras");

            migrationBuilder.DropTable(
                name: "Incapacidades");

            migrationBuilder.DropTable(
                name: "PagosFeriado");

            migrationBuilder.DropTable(
                name: "PlanillasEmpleado");

            migrationBuilder.DropTable(
                name: "Feriados");

            migrationBuilder.DropTable(
                name: "PeriodosPago");

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Puestos",
                keyColumn: "PuestoId",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "FechaIngreso",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoJornada",
                table: "Empleados");
        }
    }
}

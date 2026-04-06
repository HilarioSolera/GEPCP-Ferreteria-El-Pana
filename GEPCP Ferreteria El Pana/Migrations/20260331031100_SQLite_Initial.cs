using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GEPCP_Ferreteria_El_Pana.Migrations
{
    /// <inheritdoc />
    public partial class SQLite_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Cedula = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PrimerApellido = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SegundoApellido = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Puesto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Departamento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TipoJornada = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SalarioBase = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CorreoElectronico = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    NumeroCuenta = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    FormaPago = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.EmpleadoId);
                });

            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    FeriadoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.FeriadoId);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosPago",
                columns: table => new
                {
                    PeriodoPagoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quincena = table.Column<int>(type: "INTEGER", nullable: false),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    Anio = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosPago", x => x.PeriodoPagoId);
                });

            migrationBuilder.CreateTable(
                name: "Puestos",
                columns: table => new
                {
                    PuestoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SalarioBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puestos", x => x.PuestoId);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    RegistroAuditoriaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Usuario = table.Column<string>(type: "TEXT", nullable: false),
                    Accion = table.Column<string>(type: "TEXT", nullable: false),
                    Modulo = table.Column<string>(type: "TEXT", nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.RegistroAuditoriaId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RolId);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CorreoElectronico = table.Column<string>(type: "TEXT", nullable: true),
                    TokenRecuperacion = table.Column<string>(type: "TEXT", nullable: true),
                    TokenExpiracion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NombreUsuario = table.Column<string>(type: "TEXT", nullable: false),
                    NombreCompleto = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Rol = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "Aguinaldos",
                columns: table => new
                {
                    AguinaldoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Anio = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aguinaldos", x => x.AguinaldoId);
                    table.ForeignKey(
                        name: "FK_Aguinaldos_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comisiones",
                columns: table => new
                {
                    ComisionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comisiones", x => x.ComisionId);
                    table.ForeignKey(
                        name: "FK_Comisiones_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditosFerreteria",
                columns: table => new
                {
                    CreditoFerreteriaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Saldo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CuotaQuincenal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaCredito = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
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
                name: "HistorialSalarios",
                columns: table => new
                {
                    HistorialSalarioId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    SalarioAnterior = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SalarioNuevo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ModificadoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialSalarios", x => x.HistorialSalarioId);
                    table.ForeignKey(
                        name: "FK_HistorialSalarios_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incapacidades",
                columns: table => new
                {
                    IncapacidadId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Entidad = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoIncapacidad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalDias = table.Column<int>(type: "INTEGER", nullable: false),
                    TiqueteCCSS = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PorcentajePago = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ResponsablePago = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoPorDia = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
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
                name: "Planillas",
                columns: table => new
                {
                    PlanillaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SalarioBruto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Deducciones = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalarioNeto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Pagada = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planillas", x => x.PlanillaId);
                    table.ForeignKey(
                        name: "FK_Planillas_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prestamos",
                columns: table => new
                {
                    PrestamoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaPrestamo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Interes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cuotas = table.Column<int>(type: "INTEGER", nullable: false),
                    CuotaMensual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestamos", x => x.PrestamoId);
                    table.ForeignKey(
                        name: "FK_Prestamos_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "EmpleadoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorasExtras",
                columns: table => new
                {
                    HorasExtrasId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodoPagoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalHoras = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    ValorHora = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Porcentaje = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    PagoFeriadoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeriadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodoPagoId = table.Column<int>(type: "INTEGER", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
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
                    PlanillaEmpleadoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeriodoPagoId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpleadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    HorasOrdinarias = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    HorasExtras = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    HorasNoLaboradas = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ValorHora = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ValorHoraExtra = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SalarioOrdinario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AumentoAplicado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontoHorasExtras = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    MontoFeriados = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalDevengado = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PorcentajeCCSS = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    DeduccionCCSS = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DeduccionPrestamos = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DeduccionCreditoFerreteria = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DeduccionIncapacidad = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DeduccionHorasNoLaboradas = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OtrasDeducciones = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalDeducciones = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NetoAPagar = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "AbonoCreditoFerreteria",
                columns: table => new
                {
                    AbonoCreditoFerreteriaId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreditoFerreteriaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaAbono = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbonoCreditoFerreteria", x => x.AbonoCreditoFerreteriaId);
                    table.ForeignKey(
                        name: "FK_AbonoCreditoFerreteria_CreditosFerreteria_CreditoFerreteriaId",
                        column: x => x.CreditoFerreteriaId,
                        principalTable: "CreditosFerreteria",
                        principalColumn: "CreditoFerreteriaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbonoPrestamo",
                columns: table => new
                {
                    AbonoPrestamoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PrestamoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaAbono = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbonoPrestamo", x => x.AbonoPrestamoId);
                    table.ForeignKey(
                        name: "FK_AbonoPrestamo_Prestamos_PrestamoId",
                        column: x => x.PrestamoId,
                        principalTable: "Prestamos",
                        principalColumn: "PrestamoId",
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
                    { 1, true, "Encargada de RR.H.H.", 450000m },
                    { 2, true, "Vendedor", 380000m }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RolId", "Nombre" },
                values: new object[,]
                {
                    { 1, "RRHH" },
                    { 2, "Jefatura" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "UsuarioId", "CorreoElectronico", "NombreCompleto", "NombreUsuario", "PasswordHash", "Rol", "TokenExpiracion", "TokenRecuperacion" },
                values: new object[,]
                {
                    { 1, null, "Administrador RRHH", "admin.rrhh", "$2a$11$/mJGbQrxHo3bDUtdY6MWoeaJc/6aYPE7EG9ukr6ln9mNupX3Y8Wz.", "RRHH", null, null },
                    { 2, null, "Usuario Jefatura", "jefatura", "$2a$11$T72F0Mu8ocYejSTck6bprueMSoi5WgVtSD.hIraw5PvhnjDde6rD6", "Jefatura", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbonoCreditoFerreteria_CreditoFerreteriaId",
                table: "AbonoCreditoFerreteria",
                column: "CreditoFerreteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonoPrestamo_PrestamoId",
                table: "AbonoPrestamo",
                column: "PrestamoId");

            migrationBuilder.CreateIndex(
                name: "IX_Aguinaldos_EmpleadoId_Anio",
                table: "Aguinaldos",
                columns: new[] { "EmpleadoId", "Anio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comisiones_EmpleadoId",
                table: "Comisiones",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditosFerreteria_EmpleadoId",
                table: "CreditosFerreteria",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Cedula",
                table: "Empleados",
                column: "Cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialSalarios_EmpleadoId",
                table: "HistorialSalarios",
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
                name: "IX_Planillas_EmpleadoId",
                table: "Planillas",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanillasEmpleado_EmpleadoId_PeriodoPagoId",
                table: "PlanillasEmpleado",
                columns: new[] { "EmpleadoId", "PeriodoPagoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanillasEmpleado_PeriodoPagoId",
                table: "PlanillasEmpleado",
                column: "PeriodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_EmpleadoId",
                table: "Prestamos",
                column: "EmpleadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbonoCreditoFerreteria");

            migrationBuilder.DropTable(
                name: "AbonoPrestamo");

            migrationBuilder.DropTable(
                name: "Aguinaldos");

            migrationBuilder.DropTable(
                name: "Comisiones");

            migrationBuilder.DropTable(
                name: "HistorialSalarios");

            migrationBuilder.DropTable(
                name: "HorasExtras");

            migrationBuilder.DropTable(
                name: "Incapacidades");

            migrationBuilder.DropTable(
                name: "PagosFeriado");

            migrationBuilder.DropTable(
                name: "Planillas");

            migrationBuilder.DropTable(
                name: "PlanillasEmpleado");

            migrationBuilder.DropTable(
                name: "Puestos");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "CreditosFerreteria");

            migrationBuilder.DropTable(
                name: "Prestamos");

            migrationBuilder.DropTable(
                name: "Feriados");

            migrationBuilder.DropTable(
                name: "PeriodosPago");

            migrationBuilder.DropTable(
                name: "Empleados");
        }
    }
}

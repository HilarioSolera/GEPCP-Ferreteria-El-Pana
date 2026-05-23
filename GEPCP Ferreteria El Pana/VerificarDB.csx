using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;

var connectionString = "Data Source=GEPCP_Ferreteria_El_Pana.db";
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlite(connectionString)
    .Options;

using var context = new ApplicationDbContext(options);

Console.WriteLine("=== VERIFICACIÓN DE BASE DE DATOS ===\n");

// Verificar usuarios
var usuarios = context.Usuarios.ToList();
Console.WriteLine($"Usuarios: {usuarios.Count}");
foreach (var u in usuarios)
{
    Console.WriteLine($"  - {u.NombreUsuario} ({u.Rol})");
}

// Verificar períodos
var periodos = context.PeriodosPago.ToList();
Console.WriteLine($"\nPeríodos de pago: {periodos.Count}");
foreach (var p in periodos.Take(5))
{
    Console.WriteLine($"  - {p.Descripcion} ({p.Estado})");
}

// Verificar empleados
var empleados = context.Empleados.ToList();
Console.WriteLine($"\nEmpleados: {empleados.Count}");

// Verificar planillas
var planillas = context.PlanillasEmpleado.ToList();
Console.WriteLine($"\nPlanillas: {planillas.Count}");

Console.WriteLine("\n=== FIN ===");

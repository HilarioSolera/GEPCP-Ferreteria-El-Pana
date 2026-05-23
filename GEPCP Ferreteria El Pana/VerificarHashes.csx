using System;
using BCrypt.Net;

// VERIFICAR HASHES DE CONTRASEÑAS

Console.WriteLine("=== VERIFICACIÓN DE CREDENCIALES BCrypt ===\n");

// Hashes almacenados en la base de datos
string hashRRHH = "$2a$11$y6tuNj6/4sGOWx2WhAP9IO4vqO9P1kHIWNZUYdi1yLM1VagXKIlbS";
string hashJefatura = "$2a$11$QwnWnzunvEvMfARNsI5xmuvqqiymZKd3TqVT1QsXCEQjODq5htYhG";

// Contraseñas que deberían funcionar
string passRRHH = "Rrhh2024!";
string passJefatura = "Jefe2024!";

Console.WriteLine("1. Usuario: admin.rrhh");
Console.WriteLine($"   Contraseña: {passRRHH}");
Console.WriteLine($"   Hash en BD: {hashRRHH}");
bool validRRHH = BCrypt.Verify(passRRHH, hashRRHH);
Console.WriteLine($"   ✓ Validación: {(validRRHH ? "CORRECTA ✓" : "INCORRECTA ✗")}\n");

Console.WriteLine("2. Usuario: jefatura");
Console.WriteLine($"   Contraseña: {passJefatura}");
Console.WriteLine($"   Hash en BD: {hashJefatura}");
bool validJefatura = BCrypt.Verify(passJefatura, hashJefatura);
Console.WriteLine($"   ✓ Validación: {(validJefatura ? "CORRECTA ✓" : "INCORRECTA ✗")}\n");

if (validRRHH && validJefatura)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✓✓✓ TODOS LOS HASHES SON CORRECTOS ✓✓✓");
    Console.ResetColor();
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("✗✗✗ HAY PROBLEMAS CON LOS HASHES ✗✗✗");
    Console.ResetColor();

    // Generar nuevos hashes
    Console.WriteLine("\n=== GENERANDO NUEVOS HASHES ===\n");
    string nuevoHashRRHH = BCrypt.HashPassword(passRRHH);
    string nuevoHashJefatura = BCrypt.HashPassword(passJefatura);

    Console.WriteLine($"Nuevo hash para admin.rrhh: {nuevoHashRRHH}");
    Console.WriteLine($"Nuevo hash para jefatura: {nuevoHashJefatura}");
}

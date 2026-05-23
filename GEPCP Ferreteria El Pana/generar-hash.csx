// Ejecutar en consola interactiva C# para generar hashes BCrypt
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

var password = "Pana2024";
var hash = BCrypt.HashPassword(password, 11);
Console.WriteLine($"Contraseña: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine($"Verificación: {BCrypt.Verify(password, hash)}");

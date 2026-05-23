# Script de Verificación - Cierre Automático del Servidor
# Ejecuta este script DESPUÉS de cerrar la ventana del navegador

Write-Host "`n🔍 VERIFICANDO ESTADO DEL SERVIDOR...`n" -ForegroundColor Cyan

# Esperar 3 segundos para dar tiempo al cierre
Write-Host "⏳ Esperando 3 segundos..." -ForegroundColor Yellow
Start-Sleep 3

# Verificar si hay procesos GEPCP corriendo
$procesos = Get-Process -Name "GEPCP*" -ErrorAction SilentlyContinue

if ($procesos) {
	Write-Host "❌ PROBLEMA: El servidor AÚN está corriendo`n" -ForegroundColor Red
	Write-Host "Procesos encontrados:" -ForegroundColor Yellow
	$procesos | Format-Table -Property Id, ProcessName, StartTime -AutoSize

	Write-Host "`n📋 DIAGNÓSTICO:" -ForegroundColor Yellow
	Write-Host "  1. El script de cierre automático NO funcionó"
	Write-Host "  2. Posibles causas:"
	Write-Host "     - El navegador no ejecutó el script beforeunload"
	Write-Host "     - La petición a /api/server/shutdown no llegó"
	Write-Host "     - El endpoint no está registrado correctamente"

	Write-Host "`n🔧 SOLUCIÓN TEMPORAL:" -ForegroundColor Cyan
	Write-Host "  Ejecuta: Stop-Process -Name 'GEPCP*' -Force"

	$respuesta = Read-Host "`n¿Quieres detener los procesos ahora? (S/N)"
	if ($respuesta -eq "S" -or $respuesta -eq "s") {
		Stop-Process -Name "GEPCP*" -Force
		Write-Host "✅ Procesos detenidos manualmente" -ForegroundColor Green
	}
} else {
	Write-Host "✅ ÉXITO: El servidor se detuvo correctamente`n" -ForegroundColor Green
	Write-Host "El cierre automático está funcionando ✓" -ForegroundColor Green
}

# Verificar puerto
Write-Host "`n🔌 Verificando puerto 5002..." -ForegroundColor Cyan
$puerto = netstat -ano | Select-String "5002"

if ($puerto) {
	Write-Host "⚠️ El puerto 5002 aún está ocupado:" -ForegroundColor Yellow
	$puerto | ForEach-Object { Write-Host "  $_" }
} else {
	Write-Host "✅ Puerto 5002 liberado" -ForegroundColor Green
}

Write-Host "`n" 
Read-Host "Presiona Enter para salir"

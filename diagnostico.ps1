# Script de diagnóstico para GEPCP Ferretería El Pana

Write-Host "╔═══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   DIAGNÓSTICO - GEPCP FERRETERÍA EL PANA              ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host ""
Write-Host "1. Verificando archivos publicados..." -ForegroundColor Yellow

$publishPath = 'C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana\publish'
$exePath = Join-Path $publishPath 'GEPCP Ferreteria El Pana.exe'

if (Test-Path $exePath) {
	$size = (Get-Item $exePath).Length / 1MB
	Write-Host "✅ EXE encontrado: $exePath" -ForegroundColor Green
	Write-Host "   Tamaño: $([Math]::Round($size, 2)) MB" -ForegroundColor Green
} else {
	Write-Host "❌ EXE NO encontrado en: $exePath" -ForegroundColor Red
	exit
}

Write-Host ""
Write-Host "2. Verificando puerto 5002..." -ForegroundColor Yellow

$portInUse = Get-NetTCPConnection -LocalPort 5002 -ErrorAction SilentlyContinue
if ($portInUse) {
	Write-Host "⚠️  Puerto 5002 ya está en uso" -ForegroundColor Yellow
	Write-Host "   Proceso: $($portInUse.OwningProcess)" -ForegroundColor Yellow
	Write-Host "   Necesitas cerrar esa aplicación primero" -ForegroundColor Yellow
} else {
	Write-Host "✅ Puerto 5002 disponible" -ForegroundColor Green
}

Write-Host ""
Write-Host "3. Iniciando aplicación..." -ForegroundColor Yellow
Write-Host ""

try {
	& $exePath
} catch {
	Write-Host "❌ Error ejecutando EXE: $_" -ForegroundColor Red
}

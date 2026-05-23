# Script para generar favicon.ico de alta calidad desde logo-el-pana.jpg

Write-Host "=== GENERADOR DE FAVICON ===" -ForegroundColor Cyan
Write-Host ""

$projectRoot = "C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana\GEPCP Ferreteria El Pana"
$logoPath = Join-Path $projectRoot "wwwroot\images\logo-el-pana.jpg"
$faviconPath = Join-Path $projectRoot "wwwroot\favicon.ico"

if (-not (Test-Path $logoPath)) {
	Write-Host "[ERROR] No se encontró el logo en: $logoPath" -ForegroundColor Red
	exit 1
}

Write-Host "[INFO] Logo encontrado: $logoPath" -ForegroundColor Green
Write-Host ""
Write-Host "OPCIONES PARA GENERAR FAVICON.ICO:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. ONLINE (Recomendado - Más fácil):" -ForegroundColor White
Write-Host "   - Ir a: https://www.icoconverter.com/"
Write-Host "   - Subir: $logoPath"
Write-Host "   - Seleccionar tamaños: 16x16, 32x32, 48x48, 256x256"
Write-Host "   - Descargar y reemplazar en: $faviconPath"
Write-Host ""
Write-Host "2. IMAGEMAGICK (Línea de comandos):" -ForegroundColor White
Write-Host "   Instalar ImageMagick: winget install ImageMagick.ImageMagick"
Write-Host "   Luego ejecutar:"
Write-Host "   magick '$logoPath' -resize 256x256 '$faviconPath'"
Write-Host ""
Write-Host "3. PAINT.NET o GIMP (Manual):" -ForegroundColor White
Write-Host "   - Abrir '$logoPath'"
Write-Host "   - Redimensionar a 256x256 (mantener proporciones)"
Write-Host "   - Exportar como .ico con múltiples resoluciones"
Write-Host "   - Guardar en '$faviconPath'"
Write-Host ""
Write-Host "NOTA: Actualmente el sistema usa el JPG directamente como favicon," -ForegroundColor Cyan
Write-Host "      que funciona perfectamente en navegadores modernos." -ForegroundColor Cyan
Write-Host ""

# Intentar abrir la herramienta online
$response = Read-Host "¿Desea abrir la herramienta online ahora? (S/N)"
if ($response -eq "S" -or $response -eq "s") {
	Start-Process "https://www.icoconverter.com/"
	Write-Host "[OK] Navegador abierto. Siga las instrucciones anteriores." -ForegroundColor Green
}

Write-Host ""
Write-Host "=== FIN ===" -ForegroundColor Cyan

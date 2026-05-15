# ═══════════════════════════════════════════════════════════════════
# SCRIPT MAESTRO DE PUBLICACIÓN
# GEPCP Ferretería El Pana - Sistema de Gestión de Planillas
# ═══════════════════════════════════════════════════════════════════

param(
	[switch]$SoloZip,
	[switch]$SoloExe,
	[switch]$ConInnoSetup
)

$ErrorActionPreference = "Stop"

# Configuración
$Version = "1.0.0"
$AppName = "GEPCP Ferretería El Pana"
$ProyectoPath = ".\GEPCP Ferreteria El Pana.csproj"
$PublishPath = ".\bin\Release\net8.0\win-x64\publish"
$InstaladorPath = ".\Instalador"
$OutputPath = "$InstaladorPath\Output"
$InnoSetupExe = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

# Banner
Clear-Host
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  GEPCP FERRETERÍA EL PANA" -ForegroundColor Cyan
Write-Host "  Sistema de Publicación y Empaquetado Profesional" -ForegroundColor Cyan
Write-Host "  Versión: $Version" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ══════════════════════════════════════════════════════════════════
# PASO 1: Verificar requisitos
# ══════════════════════════════════════════════════════════════════

Write-Host "[1/6] Verificando requisitos..." -ForegroundColor Yellow

# Verificar .NET SDK
try {
	$dotnetVersion = dotnet --version
	Write-Host "  ✓ .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
	Write-Host "  ✗ Error: .NET SDK no encontrado" -ForegroundColor Red
	Write-Host "  Descargar desde: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
	exit 1
}

# Verificar Inno Setup
$tieneInnoSetup = Test-Path $InnoSetupExe
if ($tieneInnoSetup) {
	Write-Host "  ✓ Inno Setup encontrado" -ForegroundColor Green
	$ConInnoSetup = $true
} else {
	Write-Host "  ⚠ Inno Setup no encontrado" -ForegroundColor Yellow
	Write-Host "    Para crear instalador .EXE, instalar desde:" -ForegroundColor Yellow
	Write-Host "    https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
	$ConInnoSetup = $false
}

Write-Host ""

# ══════════════════════════════════════════════════════════════════
# PASO 2: Limpiar publicaciones anteriores
# ══════════════════════════════════════════════════════════════════

Write-Host "[2/6] Limpiando publicaciones anteriores..." -ForegroundColor Yellow

if (Test-Path $PublishPath) {
	Remove-Item -Path $PublishPath -Recurse -Force
	Write-Host "  ✓ Carpeta publish limpiada" -ForegroundColor Green
}

if (Test-Path $OutputPath) {
	Remove-Item -Path $OutputPath -Recurse -Force
	Write-Host "  ✓ Carpeta output limpiada" -ForegroundColor Green
}

New-Item -ItemType Directory -Path $InstaladorPath -Force | Out-Null
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

Write-Host ""

# ══════════════════════════════════════════════════════════════════
# PASO 3: Publicar aplicación
# ══════════════════════════════════════════════════════════════════

Write-Host "[3/6] Publicando aplicación..." -ForegroundColor Yellow
Write-Host "  (Esto puede tardar varios minutos...)" -ForegroundColor Gray
Write-Host ""

$publishArgs = @(
	"publish"
	$ProyectoPath
	"--configuration", "Release"
	"--output", $PublishPath
	"--runtime", "win-x64"
	"--self-contained", "true"
	"-p:PublishSingleFile=false"
	"-p:PublishReadyToRun=true"
	"-p:DebugType=None"
	"-p:DebugSymbols=false"
	"-p:EnableCompressionInSingleFile=true"
)

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
	Write-Host ""
	Write-Host "  ✗ Error en la publicación" -ForegroundColor Red
	exit 1
}

# Calcular tamaño de publicación
$publishSize = (Get-ChildItem $PublishPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host ""
Write-Host "  ✓ Publicación completada: $([math]::Round($publishSize, 2)) MB" -ForegroundColor Green

Write-Host ""

# ══════════════════════════════════════════════════════════════════
# PASO 4: Crear archivos auxiliares
# ══════════════════════════════════════════════════════════════════

Write-Host "[4/6] Creando archivos de instalación..." -ForegroundColor Yellow

# IniciarSistema.bat
$batContent = @'
@echo off
title GEPCP Ferreteria El Pana
cls
echo.
echo ════════════════════════════════════════════════════════════
echo   GEPCP Ferreteria El Pana - Iniciando Sistema
echo ════════════════════════════════════════════════════════════
echo.

REM Verificar si el servicio está corriendo
sc query GEPCPFerreteriaElPana | find "RUNNING" >nul

if %errorlevel% == 0 (
	echo [OK] El servicio esta corriendo
	echo.
	echo Abriendo navegador en http://localhost:5000
	start http://localhost:5000
	timeout /t 2 >nul
	exit
)

echo El servicio no esta activo. Iniciando...
net start GEPCPFerreteriaElPana

if %errorlevel% == 0 (
	echo [OK] Servicio iniciado correctamente
	timeout /t 3 >nul
	echo.
	echo Abriendo navegador en http://localhost:5000
	start http://localhost:5000
	timeout /t 2 >nul
) else (
	echo [ERROR] No se pudo iniciar el servicio
	echo.
	echo Ejecute este archivo como Administrador o
	echo inicie el servicio desde Servicios de Windows
	echo.
	pause
)

exit
'@

$batContent | Out-File -FilePath "$PublishPath\IniciarSistema.bat" -Encoding ASCII -NoNewline
Write-Host "  ✓ IniciarSistema.bat creado" -ForegroundColor Green

# MANUAL_INSTALACION.txt
$manualContent = @"
═══════════════════════════════════════════════════════════════════
  GEPCP FERRETERÍA EL PANA
  Manual de Instalación y Uso - Versión $Version
═══════════════════════════════════════════════════════════════════

█ REQUISITOS DEL SISTEMA
─────────────────────────────────────────────────────────────────

  • Sistema Operativo: Windows 10/11 (64 bits)
  • Memoria RAM: 2 GB mínimo, 4 GB recomendado
  • Espacio en Disco: 500 MB libres
  • Navegador Web: Chrome, Edge o Firefox actualizado
  • Conexión a Internet: Para envío de correos

█ INSTALACIÓN CON INSTALADOR .EXE (RECOMENDADO)
─────────────────────────────────────────────────────────────────

  1. Ejecutar: GEPCP_FerreteriaElPana_Setup_v$Version.exe
  2. Seguir asistente de instalación
  3. Aceptar permisos de administrador
  4. Configurar opciones:
	 ☑ Crear acceso directo en escritorio
	 ☑ Iniciar con Windows
	 ☑ Configurar firewall
  5. Clic en "Instalar"
  6. ¡Listo! El sistema se abrirá automáticamente

█ INSTALACIÓN MANUAL (SIN INSTALADOR)
─────────────────────────────────────────────────────────────────

  1. Extraer contenido a: C:\Program Files\GEPCP Ferretería El Pana

  2. Abrir PowerShell como Administrador

  3. Ejecutar comandos:

	 cd "C:\Program Files\GEPCP Ferretería El Pana"

	 sc.exe create GEPCPFerreteriaElPana binPath= "`$PWD\GEPCP Ferreteria El Pana.exe" start= auto DisplayName= "GEPCP Ferretería El Pana"

	 sc.exe description GEPCPFerreteriaElPana "Sistema de gestión de planillas y RRHH"

	 sc.exe failure GEPCPFerreteriaElPana reset= 86400 actions= restart/60000

	 netsh advfirewall firewall add rule name="GEPCP Ferretería El Pana" dir=in action=allow protocol=TCP localport=5000

	 .\GEPCP` Ferreteria` El` Pana.exe --migrate-database

	 sc.exe start GEPCPFerreteriaElPana

█ ACCESO AL SISTEMA
─────────────────────────────────────────────────────────────────

  URL: http://localhost:5000

  FORMAS DE INICIAR:
  • Doble clic en acceso directo del escritorio
  • Doble clic en: IniciarSistema.bat
  • El servicio inicia automáticamente con Windows

█ CONFIGURACIÓN DE CORREO ELECTRÓNICO
─────────────────────────────────────────────────────────────────

  Para enviar boletas y reportes por correo:

  1. Editar: appsettings.Production.json
  2. Buscar sección "Email"
  3. Completar:

	 "Email": {
	   "Host": "smtp.gmail.com",
	   "Port": 587,
	   "Usuario": "tu-correo@gmail.com",
	   "Password": "tu-contraseña-de-aplicación",
	   "Nombre": "GEPCP Ferretería El Pana"
	 }

  ⚠ IMPORTANTE para Gmail:
  • Usar "Contraseña de aplicación", NO la contraseña normal
  • Generar en: https://myaccount.google.com/apppasswords

  4. Reiniciar servicio:
	 PowerShell (Admin): Restart-Service GEPCPFerreteriaElPana

█ GESTIÓN DEL SERVICIO
─────────────────────────────────────────────────────────────────

  INICIAR:   net start GEPCPFerreteriaElPana
  DETENER:   net stop GEPCPFerreteriaElPana
  REINICIAR: Restart-Service GEPCPFerreteriaElPana
  ESTADO:    sc query GEPCPFerreteriaElPana

  (Ejecutar PowerShell como Administrador)

█ RESPALDO DE DATOS (IMPORTANTE)
─────────────────────────────────────────────────────────────────

  RECOMENDADO: Respaldo semanal

  1. Detener servicio:
	 net stop GEPCPFerreteriaElPana

  2. Copiar archivo:
	 C:\Program Files\GEPCP Ferretería El Pana\GEPCP_Ferreteria_El_Pana.db

  3. Guardar en ubicación segura:
	 • Memoria USB
	 • Google Drive / OneDrive
	 • Servidor de respaldos

  4. Reiniciar servicio:
	 net start GEPCPFerreteriaElPana

█ SOLUCIÓN DE PROBLEMAS
─────────────────────────────────────────────────────────────────

  PROBLEMA: No se puede acceder al sistema
  SOLUCIÓN:
	• Verificar servicio: sc query GEPCPFerreteriaElPana
	• Revisar logs en: C:\Program Files\...\Logs\
	• Verificar puerto 5000 libre

  PROBLEMA: No se envían correos
  SOLUCIÓN:
	• Verificar appsettings.Production.json
	• Para Gmail, usar contraseña de aplicación
	• Verificar conexión a Internet
	• Revisar logs del sistema

  PROBLEMA: Error al iniciar servicio
  SOLUCIÓN:
	• Ejecutar como Administrador
	• Verificar puerto 5000 disponible
	• Revisar Visor de Eventos de Windows

█ UBICACIÓN DE ARCHIVOS
─────────────────────────────────────────────────────────────────

  Aplicación:    C:\Program Files\GEPCP Ferretería El Pana\
  Base de Datos: C:\Program Files\...\GEPCP_Ferreteria_El_Pana.db
  Configuración: C:\Program Files\...\appsettings.Production.json
  Logs:          C:\Program Files\...\Logs\
  Manual:        C:\Program Files\...\MANUAL_INSTALACION.txt

█ DESINSTALACIÓN
─────────────────────────────────────────────────────────────────

  CON INSTALADOR:
  • Panel de Control > Programas > Desinstalar
  • Buscar "GEPCP Ferretería El Pana"
  • Opción de conservar o eliminar datos

  MANUAL:
  1. Detener servicio: net stop GEPCPFerreteriaElPana
  2. Eliminar servicio: sc.exe delete GEPCPFerreteriaElPana
  3. Eliminar carpeta (conservar .db si desea mantener datos)

█ SOPORTE TÉCNICO
─────────────────────────────────────────────────────────────────

  Email:  ferreteriaelpana2026@gmail.com
  GitHub: https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana

  Desarrollado con ❤ en Costa Rica

═══════════════════════════════════════════════════════════════════
  © 2026 GEPCP Ferretería El Pana - Todos los derechos reservados
═══════════════════════════════════════════════════════════════════
"@

$manualContent | Out-File -FilePath "$PublishPath\MANUAL_INSTALACION.txt" -Encoding UTF8
Write-Host "  ✓ MANUAL_INSTALACION.txt creado" -ForegroundColor Green

# LEEME.txt
$leemeContent = @"
═══════════════════════════════════════════════════════════════════
  GEPCP FERRETERÍA EL PANA - PAQUETE DE INSTALACIÓN
  Versión $Version
═══════════════════════════════════════════════════════════════════

INSTALACIÓN RÁPIDA:

  1. Si tiene archivo .EXE:
	 → Ejecutar: GEPCP_FerreteriaElPana_Setup_v$Version.exe
	 → Seguir asistente

  2. Si tiene archivo .ZIP:
	 → Extraer contenido
	 → Leer: MANUAL_INSTALACION.txt
	 → Seguir instrucciones

ACCESO AL SISTEMA:
  → http://localhost:5000

DOCUMENTACIÓN:
  → MANUAL_INSTALACION.txt (completo)

SOPORTE:
  → ferreteriaelpana2026@gmail.com

═══════════════════════════════════════════════════════════════════
"@

$leemeContent | Out-File -FilePath "$PublishPath\LEEME.txt" -Encoding UTF8
Write-Host "  ✓ LEEME.txt creado" -ForegroundColor Green

Write-Host ""

# ══════════════════════════════════════════════════════════════════
# PASO 5: Crear paquete ZIP portátil
# ══════════════════════════════════════════════════════════════════

if (-not $SoloExe) {
	Write-Host "[5/6] Creando paquete ZIP portátil..." -ForegroundColor Yellow

	$zipPath = "$OutputPath\GEPCP_FerreteriaElPana_v${Version}_Portable.zip"

	Compress-Archive -Path "$PublishPath\*" `
		-DestinationPath $zipPath `
		-Force `
		-CompressionLevel Optimal

	$zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
	Write-Host "  ✓ ZIP creado: $zipSize MB" -ForegroundColor Green
	Write-Host "    $zipPath" -ForegroundColor Gray
	Write-Host ""
} else {
	Write-Host "[5/6] Omitiendo creación de ZIP (opción -SoloExe)" -ForegroundColor Gray
	Write-Host ""
}

# ══════════════════════════════════════════════════════════════════
# PASO 6: Crear instalador EXE con Inno Setup
# ══════════════════════════════════════════════════════════════════

if ($ConInnoSetup -and -not $SoloZip) {
	Write-Host "[6/6] Compilando instalador EXE con Inno Setup..." -ForegroundColor Yellow

	$issPath = "$InstaladorPath\Setup.iss"

	if (Test-Path $issPath) {
		& $InnoSetupExe $issPath

		if ($LASTEXITCODE -eq 0) {
			$exeFile = Get-ChildItem "$OutputPath\*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1

			if ($exeFile) {
				$exeSize = [math]::Round($exeFile.Length / 1MB, 2)
				Write-Host "  ✓ Instalador EXE creado: $exeSize MB" -ForegroundColor Green
				Write-Host "    $($exeFile.FullName)" -ForegroundColor Gray
			} else {
				Write-Host "  ⚠ Instalador compilado pero no encontrado" -ForegroundColor Yellow
			}
		} else {
			Write-Host "  ✗ Error al compilar instalador" -ForegroundColor Red
		}
	} else {
		Write-Host "  ⚠ Archivo Setup.iss no encontrado" -ForegroundColor Yellow
	}

	Write-Host ""
} else {
	if ($SoloZip) {
		Write-Host "[6/6] Omitiendo creación de instalador EXE (opción -SoloZip)" -ForegroundColor Gray
	} else {
		Write-Host "[6/6] Inno Setup no disponible - Solo se creó ZIP" -ForegroundColor Yellow
		Write-Host "  Para crear instalador EXE, instalar Inno Setup desde:" -ForegroundColor Yellow
		Write-Host "  https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
	}
	Write-Host ""
}

# ══════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ══════════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✓ PUBLICACIÓN COMPLETADA EXITOSAMENTE" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

Write-Host "ARCHIVOS GENERADOS:" -ForegroundColor Cyan
Write-Host ""

Get-ChildItem $OutputPath -File | ForEach-Object {
	$size = [math]::Round($_.Length / 1MB, 2)
	$icon = if ($_.Extension -eq ".zip") { "📦" } else { "💿" }
	Write-Host "  $icon $($_.Name)" -ForegroundColor Yellow
	Write-Host "     Tamaño: $size MB" -ForegroundColor Gray
	Write-Host "     Ruta: $($_.FullName)" -ForegroundColor DarkGray
	Write-Host ""
}

Write-Host "DISTRIBUCIÓN:" -ForegroundColor Cyan
Write-Host "  • ZIP: Instalación manual (cualquier PC)" -ForegroundColor White
Write-Host "  • EXE: Instalación automática (recomendado)" -ForegroundColor White
Write-Host ""

Write-Host "PRÓXIMOS PASOS:" -ForegroundColor Cyan
Write-Host "  1. Copiar archivo(s) a USB o compartir por email/nube" -ForegroundColor White
Write-Host "  2. En PC destino, ejecutar instalador" -ForegroundColor White
Write-Host "  3. Seguir instrucciones del asistente" -ForegroundColor White
Write-Host "  4. ¡Listo! Sistema funcionando" -ForegroundColor White
Write-Host ""

# Abrir carpeta de salida
$respuesta = Read-Host "¿Abrir carpeta con archivos generados? (S/N)"
if ($respuesta -eq "S" -or $respuesta -eq "s" -or $respuesta -eq "") {
	Invoke-Item $OutputPath
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Gracias por usar GEPCP Ferretería El Pana" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

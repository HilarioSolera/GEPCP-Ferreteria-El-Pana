@echo off
REM Script completo: Convertir logo a ICO y compilar instalador

setlocal enabledelayedexpansion
title GEPCP - Compilación Completa con Logo

echo ====================================
echo GEPCP - Compilador Completo
echo ====================================
echo.

REM PASO 1: Convertir logo a ICO
echo PASO 1: Convertiendo logo a ICO...
echo.

REM Verificar si existe Python
python --version > nul 2>&1
if %ERRORLEVEL% equ 0 (
	echo [INFO] Usando Python para convertir imagen...
	python convert-to-ico.py
	if !ERRORLEVEL! neq 0 (
		echo [ADVERTENCIA] No se pudo convertir con Python. Continuando...
	)
) else (
	echo [INFO] Python no disponible. Buscando ImageMagick...
	where magick > nul 2>&1
	if !ERRORLEVEL! equ 0 (
		echo [INFO] Usando ImageMagick para convertir imagen...
		magick convert "wwwroot\images\logo-el-pana.jpg" -define icon:auto-resize=256,128,96,64,48,32,16 "GEPCP.ico"
		if !ERRORLEVEL! neq 0 (
			echo [ADVERTENCIA] No se pudo convertir con ImageMagick. Continuando...
		)
	) else (
		echo [ADVERTENCIA] Python e ImageMagick no disponibles.
		echo [NOTA] Descarga convertio.co/jpg-ico y crea GEPCP.ico manualmente.
	)
)

if not exist "GEPCP.ico" (
	echo [ADVERTENCIA] GEPCP.ico no existe. Creando uno básico...
	REM Si falla todo, seguimos de todas formas con el instalador
)

echo.

REM PASO 2: Compilar la aplicación
echo PASO 2: Compilando aplicación .NET 8...
call dotnet build --configuration Release --no-restore

if !ERRORLEVEL! neq 0 (
	echo [ERROR] La compilación falló.
	pause
	exit /b 1
)

echo [OK] Compilación exitosa.
echo.

REM PASO 3: Crear carpeta Output si no existe
if not exist "Output" mkdir Output

REM PASO 4: Compilar el instalador
echo PASO 3: Generando instalador con Inno Setup...

REM Verificar que Inno Setup está instalado
if not exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
	if not exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
		echo [ERROR] Inno Setup 6 no está instalado.
		echo Descargue desde: https://jrsoftware.org/isdl.php
		pause
		exit /b 1
	)
	set INNO_PATH=C:\Program Files\Inno Setup 6
) else (
	set INNO_PATH=C:\Program Files (x86)\Inno Setup 6
)

echo [OK] Inno Setup encontrado en: !INNO_PATH!
echo.

"!INNO_PATH!\ISCC.exe" GEPCP.iss

if !ERRORLEVEL! neq 0 (
	echo [ERROR] No se pudo generar el instalador.
	pause
	exit /b 1
)

echo.
echo ====================================
echo [OK] INSTALADOR GENERADO EXITOSAMENTE
echo ====================================
echo.
echo Ubicación: %CD%\Output\GEPCP-Ferreteria-El-Pana-Setup.exe
echo Icono: %CD%\GEPCP.ico
echo.
echo El instalador incluye:
echo - Aplicación compilada
echo - Logo de El Pana como icono
echo - Script de inicio automático
echo - Accesos directos con el logo
echo.
pause

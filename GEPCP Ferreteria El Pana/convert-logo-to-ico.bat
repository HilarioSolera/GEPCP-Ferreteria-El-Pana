@echo off
REM Script para convertir logo-el-pana.jpg a .ico usando ImageMagick o equivalente
REM Si no tienes ImageMagick, descárgalo desde: https://imagemagick.org/

setlocal enabledelayedexpansion
title Convertir Logo a ICO

echo ====================================
echo Convertir Logo El Pana a ICO
echo ====================================
echo.

REM Verificar si la imagen existe
if not exist "wwwroot\images\logo-el-pana.jpg" (
	echo [ERROR] No se encontró: wwwroot\images\logo-el-pana.jpg
	echo.
	echo Asegúrate de que la imagen está en la ruta correcta.
	pause
	exit /b 1
)

echo [OK] Logo encontrado: wwwroot\images\logo-el-pana.jpg
echo.

REM Intentar convertir con ImageMagick (si está instalado)
echo Intentando convertir con ImageMagick...
where magick > nul 2>&1
if %ERRORLEVEL% equ 0 (
	echo Usando ImageMagick...
	magick convert "wwwroot\images\logo-el-pana.jpg" -define icon:auto-resize=256,128,96,64,48,32,16 "GEPCP.ico"
	if !ERRORLEVEL! equ 0 (
		echo [OK] Icono generado: GEPCP.ico
		goto SUCCESS
	)
)

REM Si ImageMagick no está, intentar con Python
echo Intentando con Python...
python --version > nul 2>&1
if %ERRORLEVEL% equ 0 (
	echo Usando Python...
	python -m pip install Pillow > nul 2>&1
	python convert-to-ico.py
	if !ERRORLEVEL! equ 0 (
		echo [OK] Icono generado: GEPCP.ico
		goto SUCCESS
	)
)

echo.
echo [ERROR] No se pudo generar el icono.
echo Opciones:
echo 1. Instalar ImageMagick: https://imagemagick.org/
echo 2. Instalar Python + Pillow: https://www.python.org/
echo 3. Convertir manualmente en: https://convertio.co/jpg-ico/
echo.
pause
exit /b 1

:SUCCESS
echo.
echo ====================================
echo Icono generado correctamente
echo ====================================
echo Ubicación: %CD%\GEPCP.ico
echo.
pause

@echo off
REM Script para construir el instalador con Inno Setup
REM Este script compila la aplicación y genera el Setup.exe

setlocal enabledelayedexpansion
title Compilar Instalador GEPCP - Ferretería El Pana

echo ====================================
echo GEPCP - Compilador de Instalador
echo ====================================
echo.

REM Verificar que Inno Setup está instalado
echo Verificando Inno Setup...
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

REM Compilar la aplicación
echo Compilando aplicación .NET 8...
call dotnet build --configuration Release --no-restore

if !ERRORLEVEL! neq 0 (
	echo [ERROR] La compilación falló.
	pause
	exit /b 1
)

echo [OK] Compilación exitosa.
echo.

REM Crear carpeta Output si no existe
if not exist "Output" mkdir Output

REM Compilar el instalador
echo Generando instalador...
"!INNO_PATH!\ISCC.exe" GEPCP.iss

if !ERRORLEVEL! neq 0 (
	echo [ERROR] No se pudo generar el instalador.
	pause
	exit /b 1
)

echo.
echo ====================================
echo [OK] Instalador generado exitosamente
echo ====================================
echo.
echo Ubicación: %CD%\Output\GEPCP-Ferreteria-El-Pana-Setup.exe
echo.
pause

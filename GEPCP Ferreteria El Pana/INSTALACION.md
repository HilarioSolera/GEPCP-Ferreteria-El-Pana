# GEPCP Ferretería El Pana - Guía de Instalación

## Información General

GEPCP es un sistema de gestión de recursos humanos y nómina para la Ferretería El Pana, desarrollado con .NET 8 y ASP.NET Core.

**Versión:** 1.0  
**Plataforma:** Windows 8.1+  
**Requisitos:** .NET 8 Runtime

---

## Requisitos del Sistema

### Antes de instalar:

1. ✅ **.NET 8 Runtime** - Descargar desde: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
2. ✅ **curl** - Generalmente viene incluido en Windows 10/11. Para versiones anteriores, descargar desde: https://curl.se/download.html
3. ✅ **Puerto 5002** - Debe estar disponible (el servidor usa este puerto)
4. ✅ **Permisos de administrador** - El instalador requiere permisos admin

---

## Instalación

### Opción 1: Usar el Instalador (Recomendado)

1. Descargar `GEPCP-Ferreteria-El-Pana-Setup.exe`
2. Hacer doble clic para ejecutar el instalador
3. Seguir los pasos del asistente
4. El instalador creará:
   - Acceso directo en el Escritorio
   - Acceso directo en el Menú Inicio
   - Carpeta `C:\Program Files\GEPCP Ferreteria El Pana`

### Opción 2: Instalación Manual

1. Crear carpeta: `C:\GEPCP`
2. Copiar todos los archivos compilados (`.dll`, `.exe`, `wwwroot`, etc.)
3. Copiar `IniciarSistema.bat` a la carpeta
4. Crear acceso directo apuntando a `IniciarSistema.bat`

---

## Uso

### Iniciar el sistema:

1. **Doble clic** en el acceso directo "GEPCP Ferretería El Pana"
2. El script:
   - Verifica si el servidor ya está corriendo
   - Si está activo → lo apaga y reinicia
   - Si está inactivo → lo inicia
   - Espera a que el servidor esté listo (máx 20 segundos)
   - Abre el navegador en `http://localhost:5002`

### Cerrar el sistema:

- **Opción 1:** Hacer clic en "Cerrar Sistema" en el navbar de la aplicación
- **Opción 2:** Cerrar la ventana del navegador (el servidor sigue corriendo en background)
- **Opción 3:** Presionar Ctrl+C en la ventana del script batch

---

## Estructura del Script

### IniciarSistema.bat

Este script automatiza el ciclo de vida del servidor:

```batch
┌─────────────────────────────────────┐
│ 1. Verificar si servidor está activo│ → curl /api/server/ping
├─────────────────────────────────────┤
│ 2. Si SÍ → Apagarlo                 │ → curl /api/server/shutdown
├─────────────────────────────────────┤
│ 3. Iniciar servidor                 │ → GEPCP Ferreteria El Pana.exe
├─────────────────────────────────────┤
│ 4. Esperar a que esté listo         │ → loop con curl hasta 200 OK
├─────────────────────────────────────┤
│ 5. Abrir navegador                  │ → start "" http://localhost:5002
└─────────────────────────────────────┘
```

### Endpoints del Servidor

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/api/server/ping` | GET, POST | Verifica si el servidor está activo |
| `/api/server/shutdown` | POST | Apaga el servidor de forma segura |

---

## Solución de Problemas

### ❌ El instalador no inicia

**Problema:** `GEPCP-Ferreteria-El-Pana-Setup.exe` no abre  
**Solución:**
- Ejecutar como Administrador (botón derecho → Ejecutar como administrador)
- Verificar que .NET 8 Runtime está instalado
- Intentar descargar nuevamente el instalador

---

### ❌ El servidor no inicia

**Problema:** La ventana del script se cierra o muestra error  
**Solución:**

1. Verificar que el puerto 5002 está disponible:
   ```cmd
   netstat -ano | findstr :5002
   ```
   Si hay algo escuchando, ejecutar:
   ```cmd
   taskkill /PID [PID_number] /F
   ```

2. Verificar que curl está en el PATH:
   ```cmd
   curl --version
   ```
   Si no funciona, agregar la ruta de curl al PATH de Windows

3. Revisar que .NET 8 está instalado:
   ```cmd
   dotnet --version
   ```

---

### ❌ El navegador no abre

**Problema:** El script dice que el servidor está listo pero no abre el navegador  
**Solución:**
- Abrir manualmente: `http://localhost:5002`
- Verificar que el navegador predeterminado está configurado en Windows
- Revisar los permisos del firewall

---

### ❌ Error "curl no está disponible"

**Problema:** El instalador advierte que curl no está en el PATH  
**Solución:**

1. Descargar curl desde: https://curl.se/download.html
2. Extraer en `C:\curl`
3. Agregar a variables de entorno (PATH):
   - Abrir "Variables de entorno" en Windows
   - Path → Nuevo → `C:\curl`
   - Reiniciar el equipo
4. Verificar con `curl --version` en cmd

---

## Detalles Técnicos

### Configuración del Servidor

- **Host:** localhost
- **Puerto:** 5002
- **Base de datos:** SQLite (app.db)
- **Runtime:** .NET 8

### Archivo de Configuración

`appsettings.json` - Configuración de la aplicación

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information"
	}
  }
}
```

### Variables de Entorno

Puede configurar el puerto con:
```cmd
set ASPNETCORE_URLS=http://localhost:5002
```

---

## Desinstalación

### Con el instalador:

1. Panel de Control → Programas → Programas y características
2. Buscar "GEPCP Ferretería El Pana"
3. Click derecho → Desinstalar

### Manual:

1. Eliminar carpeta `C:\Program Files\GEPCP Ferreteria El Pana`
2. Eliminar accesos directos del Escritorio y Menú Inicio

---

## Contacto y Soporte

- **Repositorio:** https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana
- **Reportar bugs:** Crear un Issue en GitHub

---

## Changelog

### v1.0 - Lanzamiento Inicial

- Sistema de instalación con Inno Setup
- Script de inicio automático
- Gestión automática de procesos del servidor
- UI mejorada con navbar naranja y separadores
- Barra de carga visible
- Botones de "Cerrar Sistema" y "Cambiar Usuario" integrados en el navbar

---

## Licencia

Este proyecto es parte de GEPCP y está bajo licencia interna de la Ferretería El Pana.

---

**Última actualización:** 2024

# 📦 GEPCP Ferretería El Pana - Instalador Completo

## ✅ Lo que se ha configurado

### 1. **Script de Inicio** (`IniciarSistema.bat`)

✨ Características:
- ✅ Verifica si el servidor ya está corriendo
- ✅ Si está activo → lo apaga → lo reinicia limpiamente
- ✅ Si está inactivo → lo inicia directamente
- ✅ Espera a que el servidor esté listo (máx 20 segundos)
- ✅ Abre automáticamente el navegador
- ✅ Todo sin necesidad de heartbeat o monitor de inactividad

🔄 Flujo:
```
Doble clic en acceso directo
		↓
¿Servidor activo?
  ├─ SÍ  → Apagarlo → Esperar 3s → Reiniciar
  └─ NO  → Iniciar directamente
		↓
¿Servidor listo? (HTTP 200)
  ├─ SÍ  → Abrir navegador
  └─ NO  → Esperar 1s → Reintentar (máx 20s)
		↓
✅ Sistema listo
```

---

### 2. **Instalador Inno Setup** (`GEPCP.iss`)

📦 Lo que incluye:
- ✅ Instalación en `C:\Program Files\GEPCP Ferreteria El Pana`
- ✅ Acceso directo en Escritorio
- ✅ Acceso directo en Menú Inicio
- ✅ Todos los archivos de la aplicación
- ✅ Base de datos SQLite
- ✅ Archivos web (CSS, JavaScript, imágenes)
- ✅ Script IniciarSistema.bat

🎯 Ventajas:
- Interface profesional
- Requiere permisos admin
- Fácil instalación para usuarios
- Desinstalación limpia

---

### 3. **Endpoint de Control** (`ServerController.cs`)

🔌 Endpoints disponibles:

| Endpoint | Método | Propósito |
|----------|--------|----------|
| `/api/server/ping` | GET, POST | Verificar si servidor está activo |
| `/api/server/shutdown` | POST | Apagar servidor de forma segura |

🚀 Sin autenticación requerida (accesible desde el script .bat)

---

### 4. **Documentación Completa**

📖 Archivos de ayuda:
- `INSTALACION.md` - Guía para usuarios finales
- `COMPILAR-INSTALADOR.md` - Guía para desarrolladores
- `README-SETUP.txt` - Este archivo

---

## 🚀 Cómo Generar el Instalador

### Opción 1: Automático (Recomendado)

```cmd
cd "C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana"
build-installer.bat
```

### Opción 2: Manual

```cmd
REM 1. Compilar en Release
dotnet build --configuration Release --no-restore

REM 2. Ejecutar Inno Setup manualmente
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" GEPCP.iss
```

---

## 📋 Requisitos para Generar el Instalador

✅ **Visual Studio 2026** (o .NET SDK 8)  
✅ **.NET 8 Runtime** (ya tienes instalado)  
✅ **Inno Setup 6** - Descargar de: https://jrsoftware.org/isdl.php

---

## 💾 Resultado

Después de ejecutar `build-installer.bat`:

📁 `Output\GEPCP-Ferreteria-El-Pana-Setup.exe` (≈ 50-100 MB)

Este archivo se puede distribuir a cualquier computadora con Windows.

---

## 👤 Uso para Usuarios Finales

### Instalación:
1. Doble clic en `GEPCP-Ferreteria-El-Pana-Setup.exe`
2. Seguir los pasos
3. ¡Listo! Se crea acceso directo automáticamente

### Uso diario:
1. Doble clic en el acceso directo
2. El script inicia/reinicia el servidor
3. Se abre el navegador automáticamente

### Cerrar:
- Opción A: Usar "Cerrar Sistema" en la app
- Opción B: Cerrar el navegador (servidor sigue corriendo)
- Opción C: Presionar Ctrl+C en la ventana del script

---

## 🔧 Cambios Realizados en el Código

### 1. **ServerController.cs**
```csharp
[HttpPost("ping")]
[HttpGet("ping")]  // ← Agregado para que curl funcione
public IActionResult Ping()
{
	ServerMonitor.UpdateLastPing();
	return Ok(new { message = "Pong", time = DateTime.UtcNow });
}
```

### 2. **Archivos Nuevos**
```
GEPCP Ferreteria El Pana/
├─ IniciarSistema.bat          ← Script de inicio
├─ GEPCP.iss                   ← Instalador Inno Setup
├─ build-installer.bat         ← Script para generar Setup
├─ INSTALACION.md              ← Guía para usuarios
├─ COMPILAR-INSTALADOR.md      ← Guía para desarrolladores
└─ README-SETUP.txt            ← Este archivo
```

---

## ✨ Ventajas del Nuevo Sistema

### vs. Heartbeat + Monitor de Inactividad:

| Aspecto | Heartbeat | Nuevo Sistema |
|--------|-----------|---------------|
| Complejidad | Media | Simple |
| Riesgo cierre accidental | Alto | Nulo |
| Configuración requerida | Sí | No |
| Apagado durante tareas largas | Posible | No (solo en clic) |
| Reinicio limpio | No | Sí |
| Requisitos | Timer background | Script .bat |

---

## 📝 Próximos Pasos (Opcional)

### Si necesitas más funcionalidades:

1. **Autoarranque en Windows:**
   - Agregar script al scheduler de Windows
   - Ejecutar al iniciar sesión

2. **Servicio de Windows:**
   - Registrar como servicio .NET
   - Gestión automática de procesos

3. **Actualizaciones automáticas:**
   - Comprobar versión al inicio
   - Descargar e instalar actualizaciones

4. **Múltiples instancias:**
   - Permitir varios servidores en puertos diferentes
   - UI para elegir puerto

---

## 🔐 Seguridad

✅ Puntos de seguridad implementados:
- Requiere permisos admin para instalar
- Servidor solo escucha en localhost:5002
- Autenticación en la app (sin cambios)
- No se expone en internet

---

## 📞 Troubleshooting Rápido

| Problema | Solución |
|----------|----------|
| Setup no abre | Ejecutar como Admin |
| Puerto 5002 ocupado | `netstat -ano \| findstr :5002` + `taskkill` |
| curl no funciona | Verificar PATH → agregar ruta curl |
| Servidor no inicia | Revisar logs → verificar .NET 8 |

---

## 📚 Documentación Adicional

- **INSTALACION.md** → Para usuarios que descargan el Setup
- **COMPILAR-INSTALADOR.md** → Para desarrolladores que necesitan generar nuevas versiones
- **GitHub** → https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana

---

## ✅ Verificación Final

Antes de distribuir:

- [ ] Compilación sin errores
- [ ] `build-installer.bat` funciona
- [ ] `Output\GEPCP-Ferreteria-El-Pana-Setup.exe` se genera
- [ ] Al instalar se crean accesos directos
- [ ] Al hacer doble clic se inicia el servidor
- [ ] El navegador abre automáticamente
- [ ] La app funciona correctamente
- [ ] Los botones "Cerrar Sistema" y "Cambiar Usuario" están en el navbar
- [ ] La barra de carga es visible y naranja

---

## 🎉 ¡Listo!

El sistema de instalación está completamente configurado y listo para distribuir.

Para generar el instalador final:
```cmd
build-installer.bat
```

Luego distribuye: `Output\GEPCP-Ferreteria-El-Pana-Setup.exe`

---

**Versión:** 1.0  
**Fecha:** 2024  
**Estado:** ✅ Completo

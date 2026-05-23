# Guía para Generar el Instalador

## Requisitos Previos

Antes de generar el instalador, asegúrate de tener instalado:

1. ✅ **Visual Studio 2026** (o VS Code + CLI)
2. ✅ **.NET 8 SDK** - https://dotnet.microsoft.com/download/dotnet/8.0
3. ✅ **Inno Setup 6** - https://jrsoftware.org/isdl.php

---

## Pasos para Generar el Instalador

### 1. Preparar el Proyecto

```cmd
cd "C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana"
```

### 2. Compilar en Release

```cmd
dotnet build --configuration Release --no-restore
```

O si prefieres usar el script automático:

```cmd
build-installer.bat
```

### 3. Resultado

El archivo `GEPCP-Ferreteria-El-Pana-Setup.exe` se generará en:
```
Output\GEPCP-Ferreteria-El-Pana-Setup.exe
```

---

## Archivos Generados

El instalador incluye automáticamente:

```
bin\Release\net8.0\
  ├─ GEPCP Ferreteria El Pana.exe
  ├─ *.dll (todas las dependencias)
  ├─ appsettings.json
  └─ app.db (base de datos SQLite)

wwwroot\
  ├─ css\gepcp-theme.css (con los estilos nuevos)
  ├─ js\*.js
  ├─ images\logo-el-pana.jpg
  └─ ... (resto de recursos)

IniciarSistema.bat
```

---

## Verificación del Instalador

Después de generar el Setup:

1. Ejecutar como Administrador:
   ```cmd
   Output\GEPCP-Ferreteria-El-Pana-Setup.exe
   ```

2. Seguir los pasos del asistente

3. Verificar que:
   - ✅ Se instala en `C:\Program Files\GEPCP Ferreteria El Pana`
   - ✅ Se crea acceso directo en el Escritorio
   - ✅ Se crea acceso directo en Menú Inicio
   - ✅ Al terminar, abre automáticamente la aplicación

---

## Troubleshooting

### ❌ Error: "Inno Setup no está instalado"

**Solución:**
- Descargar e instalar desde: https://jrsoftware.org/isdl.php
- Asegurarse de que se instala en:
  - `C:\Program Files (x86)\Inno Setup 6` o
  - `C:\Program Files\Inno Setup 6`

### ❌ Error: "La compilación falló"

**Solución:**
```cmd
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### ❌ Error: "No se puede acceder a wwwroot"

**Solución:**
- Verificar que `GEPCP.iss` tiene la ruta correcta
- Abrir `GEPCP.iss` y ajustar rutas si es necesario

---

## Personalización del Instalador

Para modificar el instalador, editar `GEPCP.iss`:

### Cambiar nombre de la aplicación:
```ini
[Setup]
AppName=Tu Nombre Aquí
```

### Cambiar icono:
```ini
SetupIconFile={#SourcePath}..\ruta\a\tu\icono.ico
```

### Cambiar carpeta de instalación:
```ini
DefaultDirName={pf}\Tu Carpeta
```

### Cambiar puerto:
Editar `IniciarSistema.bat`:
```batch
set SERVER_URL=http://localhost:5003
```

---

## Publicación

Para distribuir a usuarios:

1. Generar el Setup.exe (como se describe arriba)
2. Distribui r `GEPCP-Ferreteria-El-Pana-Setup.exe`
3. Los usuarios ejecutan como Admin → se instala automáticamente
4. Se crea acceso directo automáticamente

---

## Notas Técnicas

### Flujo del Instalador

```
Usuario ejecuta Setup.exe
		↓
Inno Setup extrae archivos a C:\Program Files\GEPCP...
		↓
Se ejecuta IniciarSistema.bat al terminar
		↓
IniciarSistema.bat inicia el servidor
		↓
Se abre navegador con http://localhost:5002
		↓
Usuario ve la aplicación funcionando
```

### Seguridad

- ✅ Se requieren permisos de administrador
- ✅ Base de datos se instala con credenciales iniciales
- ✅ El servidor solo escucha en localhost:5002
- ✅ No se expone en internet sin configuración adicional

---

## Actualizaciones

Para una nueva versión:

1. Actualizar versión en `GEPCP.iss`:
   ```ini
   AppVersion=1.1
   ```

2. Actualizar versión en proyecto `.csproj`

3. Ejecutar `build-installer.bat`

4. Distribuir nuevo Setup.exe

---

## Soporte

Si tienes preguntas o problemas:

1. Revisar [INSTALACION.md](./INSTALACION.md)
2. Verificar logs en `C:\Program Files\GEPCP Ferreteria El Pana`
3. Reportar issues en: https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana

---

**Última actualización:** 2024

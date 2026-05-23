# GEPCP Ferretería El Pana - Versión Limpia

## 📋 Descripción
Sistema de gestión de planilla y recursos humanos para Ferretería El Pana.

## ✅ Estado del Proyecto
**VERSIÓN LIMPIA Y FUNCIONAL** - Todos los archivos innecesarios han sido removidos.

## 🚀 Instalación

### Ejecutar el instalador
1. Ubicar el archivo: `Instalador\Setup_GEPCP_FerreteriaElPana.exe`
2. Ejecutar como Administrador
3. Seguir el asistente de instalación
4. La aplicación se iniciará automáticamente con Windows

### Acceso al Sistema
- **URL local**: http://localhost:5002
- **Puerto**: 5002 (fijo)
- **Acceso directo**: Se crea automáticamente en el escritorio

## 👥 Usuarios Predeterminados

### Usuario RRHH
- **Usuario**: admin.rrhh
- **Contraseña**: Pana2024
- **Rol**: Recursos Humanos (puede gestionar empleados, planillas, deducciones)

### Usuario Jefatura
- **Usuario**: jefatura
- **Contraseña**: Pana2024
- **Rol**: Jefatura (puede aprobar y consultar)

## 📁 Estructura del Proyecto

```
GEPCP Ferreteria El Pana/
├── Controllers/          # Controladores MVC
├── Data/                # Contexto de base de datos
├── Filters/             # Filtros de autorización
├── Helpers/             # Utilidades y helpers
├── Migrations/          # Migraciones de Entity Framework
├── Models/              # Modelos de datos
├── Services/            # Servicios (Auth, PDF, Email, Auditoría)
├── Views/               # Vistas Razor
│   ├── Account/         # Login, recuperación
│   ├── Aguinaldo/       # Gestión de aguinaldos
│   ├── Empleados/       # CRUD de empleados
│   ├── HorasExtras/     # Registro de horas extras
│   ├── Incapacidades/   # Gestión de incapacidades
│   ├── Planilla/        # Cálculo y generación de planillas
│   ├── Splash/          # Página de carga inicial
│   ├── Usuarios/        # Gestión de usuarios
│   └── Vacaciones/      # Gestión de vacaciones
├── wwwroot/             # Archivos estáticos (CSS, JS, imágenes)
├── Program.cs           # Punto de entrada de la aplicación
└── appsettings.json     # Configuración

Instalador/
└── Setup_GEPCP_FerreteriaElPana.exe  # Instalador generado
```

## 🔧 Tecnologías Utilizadas
- **.NET 8.0** - Framework principal
- **ASP.NET Core MVC** - Patrón de diseño
- **Entity Framework Core** - ORM
- **SQLite** - Base de datos
- **BCrypt.Net** - Encriptación de contraseñas
- **QuestPDF** - Generación de PDFs
- **ClosedXML** - Exportación a Excel
- **Inno Setup** - Empaquetado del instalador

## 📦 Paquetes NuGet
```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.1.0" />
<PackageReference Include="ClosedXML" Version="0.105.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.25" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.25" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.25" />
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="10.0.8" />
<PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="8.0.23" />
<PackageReference Include="QuestPDF" Version="2026.2.3" />
```

## 🎯 Funcionalidades Principales

### Módulo de Empleados
- Alta, baja y modificación de empleados
- Registro de información personal y laboral
- Cálculo automático de salarios y deducciones

### Módulo de Planilla
- Cálculo automático de planilla quincenal/mensual
- Cálculo de deducciones (CCSS, impuesto renta, asociación)
- Generación de comprobantes en PDF
- Exportación a Excel
- **Separación visual entre Devengados y Deducciones**

### Módulo de Vacaciones
- **Las vacaciones son siempre pagadas** (no afectan la planilla actual)
- Registro y seguimiento de vacaciones
- Cálculo de días disponibles

### Módulo de Aguinaldo
- Cálculo automático de aguinaldo
- Generación de comprobantes

### Módulo de Horas Extras
- Registro de horas extras normales y dobles
- Cálculo automático de montos

### Módulo de Incapacidades
- Registro de incapacidades
- Cálculo de días y montos

### Módulo de Usuarios
- Gestión de usuarios del sistema
- Roles: RRHH y Jefatura
- Recuperación de contraseñas

## 🔐 Seguridad
- Autenticación mediante sesiones
- Contraseñas encriptadas con BCrypt
- Filtros de autorización por rol
- Auditoría de acciones

## 📊 Reportes
- Comprobantes de planilla en PDF
- Comprobantes de aguinaldo en PDF
- Exportación de planillas a Excel
- Reportes con firmas digitales

## 🛠️ Desarrollo

### Compilar el proyecto
```powershell
cd "GEPCP Ferreteria El Pana"
dotnet build -c Release
```

### Publicar
```powershell
dotnet publish -c Release -o publish
```

### Crear instalador
1. Asegurarse de tener Inno Setup 6 instalado
2. Ejecutar:
```powershell
cd Instalador
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

## 🔄 Funcionamiento

### Al instalar
1. El instalador crea la carpeta en `Program Files`
2. Configura el inicio automático con Windows
3. Crea accesos directos
4. Inicia la aplicación

### Al ejecutar
1. La aplicación se inicia en segundo plano (WinExe)
2. Abre automáticamente el navegador en http://localhost:5002
3. Muestra una página de carga (Splash)
4. Redirige al login después de 3 segundos

### Al usar
1. Iniciar sesión con usuario y contraseña
2. Navegar por los módulos según el rol
3. Realizar operaciones (registros, cálculos, reportes)
4. El sistema guarda todo en SQLite

## 📝 Base de Datos
- **Motor**: SQLite
- **Ubicación**: `%LOCALAPPDATA%\GEPCP Ferreteria El Pana\Database\ferreteria.db`
- **Migraciones**: Automáticas al iniciar
- **Seed**: Usuarios predeterminados se crean automáticamente

## ⚙️ Configuración

### appsettings.json
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Data Source=%LOCALAPPDATA%\\GEPCP Ferreteria El Pana\\Database\\ferreteria.db"
  },
  "ReglasNegocio": {
	"PorcentajeCCSS": 10.67,
	"PorcentajeAsociacion": 3.0,
	"PorcentajeOtrasDeducciones": 0.0,
	"HorasExtrasDobleMultiplicador": 2.0
  },
  "Smtp": {
	"Host": "smtp.gmail.com",
	"Port": 587,
	"Username": "",
	"Password": "",
	"EnableSsl": true
  }
}
```

## 🧹 Limpieza Realizada

### Archivos/Carpetas Eliminados
- ✅ `bin/` y `obj/` - Archivos de compilación
- ✅ `publish/` - Publicaciones anteriores
- ✅ `temp/` - Archivos temporales
- ✅ `TestHashes/` - Archivos de prueba
- ✅ `Instalador/` anteriores - Instaladores viejos
- ✅ `Launcher/` - Lanzador obsoleto
- ✅ Archivos `.log`, `.tmp`, `.bak`
- ✅ Referencias a carpetas inexistentes en `.csproj`

### Proyecto Limpio
- ✅ Compila sin errores
- ✅ Solo advertencias menores de nullability
- ✅ Todos los controladores funcionales
- ✅ Todas las vistas operativas
- ✅ Base de datos inicializable
- ✅ Instalador generado correctamente

## 📄 Licencia
Proyecto privado de Ferretería El Pana.

## 📧 Soporte
Para soporte técnico, contactar al administrador del sistema.

---
**Versión**: 1.0  
**Fecha**: 2025  
**Estado**: ✅ FUNCIONAL Y LISTO PARA PRODUCCIÓN

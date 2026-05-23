# 📦 GEPCP - Sistema de Gestión de Planillas
## Ferretería El Pana - Instalador Profesional v1.0.0

---

## 🎯 Instalación Rápida

### 1. **Ejecutar el Instalador**
```
📂 Ubicación: Instalador\Output\GEPCP_FerreteriaElPana_Setup_v1.0.0.exe
```

**Doble click en el archivo .exe y seguir el asistente de instalación.**

---

### 2. **Primer Inicio**

Al finalizar la instalación, el sistema se abrirá automáticamente:

1. ✅ Se abrirá una **ventana de consola negra** con logs del sistema
2. ✅ Se abrirá el **navegador** en http://localhost:5000
3. ✅ Verás la **pantalla de login**

**⚠️ IMPORTANTE:** NO CIERRES la ventana de consola negra. Es normal que esté abierta mientras uses el sistema.

---

## 👤 Credenciales de Acceso

### Usuario Administrador (RRHH)
- **Usuario:** `admin.rrhh`
- **Contraseña:** `Pana2024`
- **Permisos:** Acceso completo al sistema

### Usuario Jefatura
- **Usuario:** `jefatura`
- **Contraseña:** `Pana2024`
- **Permisos:** Gestión de personal y reportes

---

## 📧 Configuración de Email (Para Recuperación de Contraseña)

El sistema **YA está configurado** con:
- ✅ Servidor SMTP de Gmail
- ✅ Contraseña de aplicación válida
- ✅ Email: ferreteriaelpana2026@gmail.com

### ¿Qué pasa si necesito recuperar mi contraseña?

1. En la pantalla de login, click en **"¿Olvidaste tu contraseña?"**
2. Ingresar tu nombre de usuario
3. **Verificar la consola negra** para ver el estado del envío
4. Revisar el email `ferreteriaelpana2026@gmail.com`
5. Ingresar el código de 6 dígitos que recibiste

---

## 🖥️ Ventana de Consola (Logs del Sistema)

Cuando el sistema esté corriendo, verás una ventana negra titulada:
```
GEPCP Ferreteria El Pana - Logs
```

### ¿Qué verás en la consola?

#### Al iniciar:
```
info: Application started
info: Now listening on: http://localhost:5000
info: Database initialized successfully
```

#### Al enviar email de recuperación:
```
info: === INICIO ENVÍO DE EMAIL ===
info: Destinatario: ferreteriaelpana2026@gmail.com
info: Configuración SMTP - Host: smtp.gmail.com
info: Configuración SMTP - Puerto: 587
info: Conectando al servidor SMTP...
info: Enviando mensaje...
info: ✓ Código enviado exitosamente
info: === FIN ENVÍO DE EMAIL ===
```

**💡 Esta ventana te ayuda a diagnosticar problemas en tiempo real.**

---

## 🛠️ Accesos Directos Instalados

Después de instalar, encontrarás en el **Menú Inicio** → **GEPCP Ferretería El Pana**:

### 1. **GEPCP Ferretería El Pana** (Principal)
Inicia el sistema normalmente.

### 2. **Reparar Sistema**
Si el sistema no inicia correctamente, este script:
- ✅ Detiene procesos antiguos
- ✅ Limpia archivos temporales
- ✅ Reinicia el sistema desde cero

### 3. **Configurar Email**
Si necesitás cambiar la configuración de email (cambiar cuenta de Gmail, actualizar contraseña, etc.)

### 4. **Carpeta de Instalación**
Abre la carpeta donde está instalado el sistema:
```
C:\Program Files (x86)\GEPCP Ferretería El Pana\
```

### 5. **Desinstalar**
Desinstala completamente el sistema.

---

## 📁 Estructura de Archivos

```
C:\Program Files (x86)\GEPCP Ferretería El Pana\
│
├── GEPCP Ferreteria El Pana.exe    ← Aplicación principal
├── appsettings.Production.json     ← Configuración (email, puertos, etc.)
├── IniciarSistema.bat              ← Script de inicio
├── RepararSistema.bat              ← Script de reparación
├── ConfigurarEmail.ps1             ← Configurador de email
├── splash.html                     ← Pantalla de carga
│
├── images\
│   └── logo-el-pana.jpg            ← Logo de la empresa
│
├── Logs\                           ← Logs del sistema
│   └── [archivos de log por fecha]
│
└── GEPCP_Ferreteria_El_Pana.db    ← Base de datos SQLite
```

---

## 🔧 Configuración Avanzada

### Cambiar Puerto del Sistema

Si el puerto 5000 está ocupado, el sistema automáticamente buscará uno libre y lo mostrará en la consola.

Para forzar un puerto específico, editar:
```
C:\Program Files (x86)\GEPCP Ferretería El Pana\appsettings.Production.json
```

Buscar la sección:
```json
"Kestrel": {
  "Endpoints": {
	"Http": {
	  "Url": "http://localhost:5000"  ← Cambiar aquí
	}
  }
}
```

---

### Cambiar Configuración de Email

**Opción 1: Usar el Script (Recomendado)**
1. Menú Inicio → GEPCP Ferretería El Pana → **Configurar Email**
2. Seguir las instrucciones en pantalla

**Opción 2: Editar Manualmente**
Editar el archivo:
```
C:\Program Files (x86)\GEPCP Ferretería El Pana\appsettings.Production.json
```

Buscar la sección:
```json
"Email": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Usuario": "ferreteriaelpana2026@gmail.com",
  "Password": "upbyperxbmudcflf",
  "Nombre": "GEPCP Ferretería El Pana"
}
```

**⚠️ IMPORTANTE:** Gmail requiere una **Contraseña de Aplicación** (16 caracteres), NO tu contraseña normal.

Para generar una:
1. Ir a: https://myaccount.google.com/apppasswords
2. Crear nueva contraseña llamada "GEPCP Sistema"
3. Copiar los 16 caracteres y pegarlos en el campo `Password`

---

## 🔍 Solución de Problemas

### ❌ El sistema no inicia

**Solución:**
1. Menú Inicio → GEPCP Ferretería El Pana → **Reparar Sistema**
2. Esperar 30 segundos
3. El sistema se abrirá automáticamente

---

### ❌ Error: "No se puede conectar a localhost:5000"

**Causa:** Otro programa está usando el puerto 5000.

**Solución:**
1. El sistema automáticamente buscará un puerto libre
2. Revisar la **consola negra** para ver qué puerto usa
3. El navegador se abrirá automáticamente en el puerto correcto

---

### ❌ Error: "Error al enviar el correo"

**Causa 1:** Contraseña de Gmail incorrecta

**Solución:**
1. Revisar la **consola negra** - verás un mensaje detallado
2. Seguir las instrucciones que aparecen en la consola
3. Ejecutar: Menú Inicio → **Configurar Email**

**Causa 2:** No hay conexión a internet

**Solución:**
Verificar tu conexión a internet y volver a intentar.

---

### ❌ La consola se cierra inmediatamente

**Causa:** Error al iniciar la aplicación.

**Solución:**
1. Ejecutar: Menú Inicio → **Reparar Sistema**
2. Si persiste, revisar los logs en:
   ```
   C:\Program Files (x86)\GEPCP Ferretería El Pana\Logs\
   ```

---

### ❌ "Usuario o contraseña incorrectos" en el primer login

**Causa:** La base de datos no se inicializó correctamente.

**Solución:**
1. Cerrar el sistema (cerrar la ventana de consola)
2. Ejecutar: Menú Inicio → **Reparar Sistema**
3. Esperar 30 segundos
4. Intentar login con: `admin.rrhh` / `Pana2024`

---

## 📊 Funcionalidades del Sistema

### ✅ Gestión de Empleados
- Alta, baja y modificación de empleados
- Gestión de salarios base
- Asignación de roles y permisos

### ✅ Planillas
- Cálculo automático de salarios
- Deducciones CCSS
- Horas extras y bonificaciones
- Exportación a PDF y Excel

### ✅ Control de Horarios
- Registro de entrada/salida
- Cálculo de horas trabajadas
- Reportes de asistencia

### ✅ Vacaciones y Permisos
- Solicitud de vacaciones
- Aprobación/rechazo
- Cálculo de días disponibles
- Historial de permisos

### ✅ Reportes
- Reporte de planillas por periodo
- Reporte de asistencia
- Exportación a PDF/Excel
- Gráficos y estadísticas

### ✅ Auditoría
- Registro de todas las operaciones
- Historial de cambios
- Trazabilidad completa

### ✅ Seguridad
- Login con usuario y contraseña
- Recuperación de contraseña por email
- Sesiones seguras
- Roles y permisos

---

## 🔐 Seguridad y Respaldo

### Base de Datos
La base de datos SQLite está ubicada en:
```
C:\Program Files (x86)\GEPCP Ferretería El Pana\GEPCP_Ferreteria_El_Pana.db
```

**💡 Recomendación:** Hacer copias de seguridad periódicas de este archivo.

### Contraseñas
Todas las contraseñas están cifradas con BCrypt (algoritmo de hash seguro).

---

## 📞 Información Técnica

| Característica | Detalle |
|----------------|---------|
| **Versión** | 1.0.0 |
| **Framework** | .NET 8.0 |
| **Plataforma** | Windows x64 |
| **Base de Datos** | SQLite |
| **Puerto por defecto** | 5000 (auto-detección) |
| **Email SMTP** | Gmail (smtp.gmail.com:587) |
| **Arquitectura** | Self-contained (no requiere .NET instalado) |

---

## 📝 Notas Importantes

### ✅ Lo que SÍ incluye el instalador:
- ✅ Aplicación completa (.NET 8 incluido)
- ✅ Base de datos con usuarios por defecto
- ✅ Configuración de email lista
- ✅ Accesos directos
- ✅ Scripts de reparación
- ✅ Logo de la empresa

### ❌ Lo que NO requiere:
- ❌ Instalar .NET separadamente
- ❌ Configurar SQL Server
- ❌ Abrir puertos del firewall (usa localhost)
- ❌ Permisos especiales (excepto para instalar)

---

## 🚀 ¿Listo para Instalar?

### Pasos Finales:

1. **Ejecutar** el instalador:
   ```
   Instalador\Output\GEPCP_FerreteriaElPana_Setup_v1.0.0.exe
   ```

2. **Seguir** el asistente de instalación

3. **Iniciar sesión** con:
   - Usuario: `admin.rrhh`
   - Contraseña: `Pana2024`

4. **¡Empezar a usar el sistema!** 🎉

---

## 📖 Documentación Adicional

En la carpeta del proyecto encontrarás:

- `SOLUCION_EMAIL_GMAIL.md` - Guía detallada de configuración de email
- `CREDENCIALES_POR_DEFECTO.txt` - Credenciales de acceso
- `README_INSTALACION.md` - Esta guía

---

## 💼 Soporte

Para soporte técnico o consultas:
- **GitHub:** https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana
- **Email:** ferreteriaelpana2026@gmail.com

---

**Desarrollado con ❤️ para Ferretería El Pana**

**© 2026 - Sistema GEPCP v1.0.0**

---

## ✨ Características Destacadas

🎨 **Interfaz Profesional** - Diseño moderno y fácil de usar  
🔒 **Seguro** - Contraseñas cifradas y auditoría completa  
📧 **Recuperación de Contraseña** - Por email con código de 6 dígitos  
📊 **Reportes Completos** - PDF y Excel listos para imprimir  
🖥️ **Logs en Tiempo Real** - Diagnóstico de problemas instantáneo  
🛠️ **Auto-Reparación** - Scripts inteligentes de mantenimiento  
⚡ **Rápido** - Base de datos SQLite optimizada  
💾 **Auto-Contenido** - No requiere instalaciones adicionales  

---

**¡Gracias por elegir GEPCP! 🚀**

# 📋 NOTAS DE VERSIÓN - GEPCP Ferretería El Pana

## Versión 1.0.0 - Versión Final Estable
**Fecha de lanzamiento:** Mayo 2026

---

## 🎉 Novedades en esta Versión

### ✨ Nuevo: Sistema de Logs en Tiempo Real
- **Consola CLI siempre visible** durante la ejecución
- Mensajes detallados de todas las operaciones
- Diagnóstico de problemas en tiempo real
- Logs guardados en archivos por fecha

### ✨ Nuevo: Detección Inteligente de Errores de Email
- Detección automática de problemas con Gmail
- Mensajes de error descriptivos con soluciones
- Guía paso a paso cuando falla el envío de email
- Validación de configuración SMTP

### ✨ Nuevo: Script de Configuración de Email Mejorado
- Instrucciones claras para obtener contraseña de Gmail
- Validación automática de contraseñas de aplicación
- Link directo a la página de Google
- Limpieza automática de espacios

### ✨ Nuevo: Sistema de Auto-Reparación
- Script "Reparar Sistema" incluido
- Limpieza automática de procesos bloqueados
- Reinicio inteligente del sistema
- Detección y resolución de conflictos de puerto

### 📧 Configuración de Email Pre-Configurada
- **Email:** ferreteriaelpana2026@gmail.com
- **Contraseña de aplicación válida incluida**
- Listo para enviar emails de recuperación
- Sin configuración adicional requerida

---

## 🛠️ Mejoras Implementadas

### Instalador
- ✅ Instalador profesional con Inno Setup
- ✅ Accesos directos en Menú Inicio y Escritorio
- ✅ Desinstalación limpia y completa
- ✅ Detección y cierre de procesos antiguos
- ✅ Logo de la empresa incluido

### Inicio del Sistema
- ✅ Splash screen con logo durante carga
- ✅ Detección automática de puerto libre
- ✅ Inicio automático del navegador
- ✅ Consola de logs visible
- ✅ Mensajes de estado claros

### Seguridad
- ✅ Contraseñas cifradas con BCrypt
- ✅ Usuarios por defecto creados automáticamente
- ✅ Recuperación de contraseña por email funcional
- ✅ Auditoría completa de operaciones
- ✅ Sesiones seguras

### Base de Datos
- ✅ Inicialización automática al primer inicio
- ✅ Usuarios RRHH y Jefatura creados por defecto
- ✅ Migraciones automáticas
- ✅ Base de datos SQLite optimizada

---

## 🐛 Problemas Resueltos

### ❌ → ✅ Error: "Usuario o contraseña incorrectos" en primer inicio
**Problema:** Los usuarios por defecto no se creaban correctamente.  
**Solución:** Usuarios ahora se crean automáticamente en el primer inicio del sistema.

### ❌ → ✅ Error: "Error al enviar el correo"
**Problema:** Contraseña de Gmail incorrecta (contraseña normal en lugar de contraseña de aplicación).  
**Solución:** Contraseña de aplicación válida pre-configurada. Mensajes de error detallados con guía de solución.

### ❌ → ✅ La ventana de consola se cerraba inmediatamente
**Problema:** Imposible ver errores en tiempo real.  
**Solución:** Consola ahora permanece abierta durante toda la ejecución del sistema.

### ❌ → ✅ No se veían los errores SMTP
**Problema:** Los errores de email no eran descriptivos.  
**Solución:** Logging detallado con mensajes claros y soluciones sugeridas.

### ❌ → ✅ Puerto 5000 ocupado causaba fallo
**Problema:** Si otro programa usaba el puerto 5000, el sistema no iniciaba.  
**Solución:** Detección automática de puerto libre y redirección del navegador.

---

## 📦 Contenido del Instalador

### Archivos Principales
- `GEPCP Ferreteria El Pana.exe` - Aplicación principal
- `appsettings.Production.json` - Configuración
- `GEPCP_Ferreteria_El_Pana.db` - Base de datos SQLite

### Scripts de Utilidad
- `IniciarSistema.bat` - Script de inicio con splash
- `RepararSistema.bat` - Script de reparación automática
- `ConfigurarEmail.ps1` - Configurador de email interactivo
- `splash.html` - Pantalla de carga animada

### Recursos
- `images/logo-el-pana.jpg` - Logo de la empresa
- Todas las DLLs de .NET 8 incluidas (self-contained)
- Recursos estáticos (CSS, JS, imágenes)

---

## 🔧 Requisitos del Sistema

### Mínimos
- **Sistema Operativo:** Windows 10 (64-bit)
- **RAM:** 2 GB
- **Espacio en Disco:** 500 MB
- **Conexión a Internet:** Requerida para envío de emails

### Recomendados
- **Sistema Operativo:** Windows 11 (64-bit)
- **RAM:** 4 GB o más
- **Espacio en Disco:** 1 GB
- **Navegador:** Firefox, Chrome, Edge (última versión)

---

## 👥 Usuarios por Defecto

### Administrador RRHH
```
Usuario: admin.rrhh
Contraseña: Pana2024
Rol: RRHH (Acceso completo)
Email: ferreteriaelpana2026@gmail.com
```

### Usuario Jefatura
```
Usuario: jefatura
Contraseña: Pana2024
Rol: Jefatura (Gestión y reportes)
Email: ferreteriaelpana2026@gmail.com
```

**💡 Recomendación:** Cambiar las contraseñas después del primer inicio.

---

## 📧 Configuración de Email

### Parámetros Pre-Configurados
```
Host: smtp.gmail.com
Puerto: 587
Seguridad: TLS/SSL
Usuario: ferreteriaelpana2026@gmail.com
Password: upbyperxbmudcflf (Contraseña de aplicación válida)
```

### Funcionalidades de Email
- ✅ Recuperación de contraseña con código de 6 dígitos
- ✅ Envío de planillas en PDF por email
- ✅ Notificaciones de aprobación de vacaciones
- ✅ Alertas de sistema

---

## 📊 Funcionalidades Principales

### Gestión de Personal
- Alta, baja y modificación de empleados
- Gestión de salarios base
- Asignación de roles
- Historial de cambios

### Planillas
- Cálculo automático de salarios
- Deducciones CCSS (10.83%)
- Horas extras
- Bonificaciones
- Exportación a PDF y Excel

### Control de Horarios
- Registro de entrada/salida
- Cálculo de horas trabajadas
- Horas extras automáticas
- Reportes de asistencia

### Vacaciones y Permisos
- Solicitud y aprobación
- Cálculo de días disponibles
- Historial completo
- Exportación de reportes

### Reportes
- Planillas por periodo
- Asistencia mensual
- Exportación PDF/Excel
- Gráficos estadísticos

### Auditoría
- Registro de todas las operaciones
- Trazabilidad completa
- Filtros avanzados
- Exportación de logs

---

## 🔐 Seguridad

### Características de Seguridad
- ✅ Contraseñas cifradas con BCrypt
- ✅ Sesiones con timeout automático
- ✅ Validación de permisos por rol
- ✅ Recuperación segura de contraseña
- ✅ Auditoría de accesos
- ✅ Protección contra SQL Injection
- ✅ Validación de formularios

---

## 🚀 Rendimiento

### Optimizaciones
- Base de datos SQLite optimizada
- Caché de consultas frecuentes
- Carga perezosa de datos
- Paginación en listados grandes
- Compresión de archivos estáticos

---

## 📁 Ubicación de Archivos

### Instalación
```
C:\Program Files (x86)\GEPCP Ferretería El Pana\
```

### Base de Datos
```
C:\Program Files (x86)\GEPCP Ferretería El Pana\GEPCP_Ferreteria_El_Pana.db
```

### Logs
```
C:\Program Files (x86)\GEPCP Ferretería El Pana\Logs\
```

### Configuración
```
C:\Program Files (x86)\GEPCP Ferretería El Pana\appsettings.Production.json
```

---

## 🔄 Actualización desde Versiones Anteriores

Esta es la primera versión estable (1.0.0). No hay actualizaciones previas.

Para futuras actualizaciones:
1. Hacer backup de la base de datos
2. Desinstalar versión anterior
3. Instalar nueva versión
4. Restaurar base de datos si es necesario

---

## 🐞 Problemas Conocidos

### Ninguno
Esta versión no tiene problemas conocidos críticos.

**Si encontrás algún problema:**
1. Revisar la consola de logs
2. Revisar los archivos de log en `Logs\`
3. Ejecutar "Reparar Sistema"
4. Reportar en GitHub si persiste

---

## 🔮 Próximas Funcionalidades (Roadmap)

### Versión 1.1 (Planificada)
- [ ] Respaldo automático de base de datos
- [ ] Integración con impresoras fiscales
- [ ] Notificaciones push
- [ ] Dashboard con métricas en tiempo real
- [ ] Modo oscuro

### Versión 1.2 (Planificada)
- [ ] API REST para integraciones
- [ ] App móvil para marcado de asistencia
- [ ] Soporte multi-empresa
- [ ] Reportes personalizables

---

## 📞 Soporte y Contacto

### Repositorio GitHub
https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana

### Email de Soporte
ferreteriaelpana2026@gmail.com

### Documentación
- `README_INSTALACION_PROFESIONAL.md` - Guía completa de instalación
- `SOLUCION_EMAIL_GMAIL.md` - Guía de configuración de email
- `CREDENCIALES_POR_DEFECTO.txt` - Credenciales de acceso

---

## 👨‍💻 Equipo de Desarrollo

**Desarrollado por:** Hilario Solera  
**Para:** Ferretería El Pana  
**Framework:** .NET 8 / ASP.NET Core  
**Base de Datos:** SQLite  
**Arquitectura:** MVC con Razor Pages  

---

## 📄 Licencia

Sistema desarrollado exclusivamente para Ferretería El Pana.  
Todos los derechos reservados © 2026

---

## 🎯 Estado del Proyecto

✅ **VERSIÓN ESTABLE LISTA PARA PRODUCCIÓN**

- ✅ Todas las funcionalidades implementadas
- ✅ Configuración de email lista
- ✅ Usuarios por defecto creados
- ✅ Logging y diagnóstico completo
- ✅ Scripts de reparación incluidos
- ✅ Documentación profesional
- ✅ Instalador probado y funcional

---

**¡Gracias por usar GEPCP! 🚀**

**Versión 1.0.0 - Mayo 2026**

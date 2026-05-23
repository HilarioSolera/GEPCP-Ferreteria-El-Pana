# 🔧 SISTEMA GEPCP FERRETERÍA EL PANA - GUÍA RÁPIDA

## ✅ CREDENCIALES DE ACCESO

**AMBOS usuarios usan la misma contraseña simple:**

```
Usuario: admin.rrhh
Contraseña: Pana2024

Usuario: jefatura
Contraseña: Pana2024
```

---

## 📦 INSTALACIÓN

1. **Ejecuta el instalador:**
   ```
   GEPCP_FerreteriaElPana_Setup_v1.0.0.exe
   ```

2. **Los usuarios se crean automáticamente** al iniciar la aplicación por primera vez

3. **La base de datos se crea automáticamente** con las migraciones

4. **NO necesitas borrar nada manualmente**

---

## 🚀 PRIMER USO

1. **Doble click** en el acceso directo del escritorio
2. **Se abrirá el navegador** automáticamente
3. **Inicia sesión** con:
   - Usuario: `admin.rrhh` o `jefatura`
   - Contraseña: `Pana2024`

---

## ⚙️ CÓMO FUNCIONA

- **Los usuarios se crean al iniciar** la app (no desde migraciones)
- **Las contraseñas se hashean con BCrypt** en runtime
- **Esto garantiza** que las contraseñas SIEMPRE funcionen

---

## 🔑 CAMBIAR CONTRASEÑAS

Una vez dentro del sistema, podes cambiar las contraseñas desde el módulo de administración de usuarios.

---

## 📧 RECUPERACIÓN DE CONTRASEÑA

Para que funcione:

1. Configura una **Contraseña de aplicación** de Gmail
2. Usa el script: `C:\Program Files\GEPCP Ferreteria El Pana\ConfigurarEmail.ps1`

---

## 🛠️ SI HAY PROBLEMAS

Ejecuta el script de reparación:
```
C:\Program Files\GEPCP Ferreteria El Pana\RepararSistema.bat
```

---

## 📝 VERSIÓN

- **v1.0.0** - Mayo 2025
- Target Framework: .NET 8
- Base de datos: SQLite
- Autenticación: BCrypt

---

**Ferretería El Pana** | Sistema de Gestión de Planillas y RRHH

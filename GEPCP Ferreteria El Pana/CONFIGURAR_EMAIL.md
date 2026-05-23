# 📧 Cómo Configurar el Servicio de Email

## ⚠️ Problema Actual
El servicio de email está fallando porque **Gmail requiere una contraseña de aplicación especial**, no tu contraseña normal de Gmail.

## ✅ Solución (Paso a Paso)

### 1️⃣ Obtener Contraseña de Aplicación de Gmail

1. **Asegurate de tener verificación en dos pasos activada** en tu cuenta de Gmail
   - Si no la tenés, activala primero en: https://myaccount.google.com/signinoptions/two-step-verification

2. **Creá una contraseña de aplicación:**
   - Andá a: https://myaccount.google.com/apppasswords
   - Iniciá sesión con tu cuenta de Gmail (`construpanapana@gmail.com`)
   - En "Seleccionar app", elegí **"Correo"**
   - En "Seleccionar dispositivo", elegí **"Otro (nombre personalizado)"**
   - Escribí: **"GEPCP Ferreteria"**
   - Hacé clic en **"Generar"**

3. **Copiá la contraseña de 16 caracteres** que aparece (ejemplo: `abcd efgh ijkl mnop`)

### 2️⃣ Actualizar appsettings.json

1. Abrí el archivo: `GEPCP Ferreteria El Pana/appsettings.json`

2. Buscá la sección `"Email"` y actualizá la contraseña **sin espacios**:

```json
"Email": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Usuario": "construpanapana@gmail.com",
  "Password": "abcdefghijklmnop",  👈 PEGÁ TU CONTRASEÑA AQUÍ (sin espacios)
  "Nombre": "GEPCP Ferretería El Pana"
}
```

3. **Guardá el archivo** (Ctrl + S)

4. **Reiniciá la aplicación** para que tome la nueva configuración

### 3️⃣ Verificar que Funciona

1. Iniciá la aplicación
2. En el login, hacé clic en **"¿Olvidaste tu contraseña?"**
3. Ingresá un nombre de usuario válido
4. Hacé clic en **"Enviar código"**
5. Revisá la bandeja de entrada del correo del empleado

---

## 🔧 Configuración Actual

- **Host SMTP:** smtp.gmail.com
- **Puerto:** 587
- **Usuario:** construpanapana@gmail.com
- **Contraseña actual:** `upbyperxbmudcflf` (16 caracteres sin espacios)

Si seguís teniendo problemas:
- Verificá que la cuenta de Gmail tenga verificación en dos pasos activa
- Asegurate de copiar la contraseña completa sin espacios
- Revisá los logs de la aplicación para ver el error específico

---

## 📝 Notas Adicionales

- La contraseña de aplicación es diferente a tu contraseña normal de Gmail
- Cada contraseña de aplicación es única y solo se muestra una vez
- Si la perdiste, generá una nueva desde el mismo enlace
- Las contraseñas de aplicación no caducan a menos que las revoque manualmente

---

## 🆘 Errores Comunes

### Error: "Authentication Required"
**Causa:** La contraseña no es válida o no es una contraseña de aplicación  
**Solución:** Generá una nueva contraseña de aplicación siguiendo los pasos anteriores

### Error: "Invalid credentials"
**Causa:** La contraseña tiene espacios o caracteres extra  
**Solución:** Asegurate de quitar TODOS los espacios al pegar la contraseña

### Error: "Connection timeout"
**Causa:** Firewall o antivirus bloqueando el puerto 587  
**Solución:** Verificá la configuración de tu firewall o antivirus

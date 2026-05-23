# 🧪 INSTRUCCIONES DE PRUEBA - Ferretería El Pana

## ✅ LO QUE SE CORRIGIÓ

### Problema Original:
- ❌ Servidor iniciaba pero navegador NO se abría
- ❌ Consola se quedaba visible permanentemente
- ❌ Usuario tenía que abrir `http://localhost:5002` manualmente

### Solución Implementada:
- ✅ Uso de `IHostApplicationLifetime.ApplicationStarted` para ejecutar código **después** de que el servidor esté listo
- ✅ `Process.Start` para abrir el navegador automáticamente
- ✅ `ShowWindow` (user32.dll) para ocultar la consola después de 3 segundos
- ✅ Configuración correcta de Kestrel con `ConfigureKestrel`

---

## 🎬 FLUJO ESPERADO

```
1. Usuario hace doble clic en "GEPCP Ferreteria El Pana.exe"
   ↓
2. Consola negra aparece con:
   "Servidor iniciado en http://localhost:5002"
   ↓
3. [Después de 0.5 segundos]
   → Navegador se abre AUTOMÁTICAMENTE
   → Muestra Splash naranja con logo grande
   ↓
4. [Durante 3 segundos]
   → Splash muestra animación de carga
   → Mensaje "Iniciando sistema..."
   ↓
5. [A los 3 segundos]
   → Splash redirige a Login
   → Consola se oculta automáticamente
   ↓
6. Usuario ve el Login listo para usar
```

---

## 📋 PASOS PARA PROBAR

### Opción 1: Probar ejecutable directo (SIN instalador)

1. Navegar a:
   ```
   C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana\GEPCP Ferreteria El Pana\publish\
   ```

2. Hacer doble clic en:
   ```
   GEPCP Ferreteria El Pana.exe
   ```

3. **Verificar:**
   - ✅ Consola aparece brevemente
   - ✅ Navegador se abre automáticamente
   - ✅ Splash naranja con logo grande aparece
   - ✅ Después de 3 segundos: Login + Consola desaparece

---

### Opción 2: Probar con el instalador (Recomendado)

1. **IMPORTANTE**: Desinstalar versión anterior si existe:
   - Panel de Control → Programas → Desinstalar "GEPCP Ferreteria El Pana"

2. Navegar a:
   ```
   C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana\Instalador\Output\
   ```

3. Ejecutar:
   ```
   Setup_FerreteriaElPana.exe
   ```

4. Seguir el asistente:
   - Marcar "Crear icono en escritorio"
   - Instalar

5. Al finalizar la instalación:
   - **NO** marcar "Ejecutar GEPCP Ferreteria El Pana" (para probar desde el icono)
   - Clic en "Finalizar"

6. Hacer doble clic en el icono del escritorio

7. **Verificar el flujo completo** (ver arriba)

---

## 🐛 QUÉ HACER SI NO FUNCIONA

### Si el navegador NO se abre:

1. Verificar que la consola muestre:
   ```
   Servidor iniciado en http://localhost:5002
   Navegador abierto
   ```

2. Si no muestra "Navegador abierto":
   - El código de apertura falló
   - Abrir manualmente: http://localhost:5002

3. Si muestra "Error al abrir navegador: ...":
   - Copiar el mensaje de error exacto
   - Reportar el error

---

### Si la consola NO se oculta:

1. Verificar que la consola muestre:
   ```
   Consola oculta
   ```

2. Si no se oculta:
   - Es posible que `MainWindowHandle` sea `IntPtr.Zero`
   - La consola puede minimizarse manualmente
   - El servidor seguirá funcionando correctamente

---

### Si aparece "No se puede conectar":

1. Verificar que el puerto 5002 no esté en uso:
   ```powershell
   netstat -ano | findstr :5002
   ```

2. Si está en uso, matar el proceso:
   ```powershell
   Stop-Process -Id <PID> -Force
   ```

3. Reintentar

---

## 📝 NOTAS TÉCNICAS

### Código clave en Program.cs:

```csharp
// Configuración de Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
	serverOptions.ListenLocalhost(PUERTO_FIJO);
});

// Ejecutar después de que el servidor esté listo
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
{
	Task.Run(async () =>
	{
		await Task.Delay(500); // Esperar que el servidor responda

		// Abrir navegador
		Process.Start(new ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true
		});

		// Ocultar consola
		await Task.Delay(3000);
		var handle = Process.GetCurrentProcess().MainWindowHandle;
		if (handle != IntPtr.Zero)
		{
			ShowWindow(handle, 0);
		}
	});
});
```

---

## ✅ CHECKLIST DE VERIFICACIÓN

Después de ejecutar, verificar que:

- [ ] Consola aparece con mensaje "Servidor iniciado..."
- [ ] Navegador se abre automáticamente en menos de 1 segundo
- [ ] Splash naranja aparece con logo grande de Ferretería El Pana
- [ ] Animación de carga funciona (spinner gira)
- [ ] Mensaje "Iniciando sistema..." es visible
- [ ] Después de 3 segundos, redirige a Login
- [ ] Consola desaparece automáticamente
- [ ] Login funciona correctamente (admin.rrhh / Pana2024)

---

## 📦 UBICACIONES IMPORTANTES

### Ejecutable publicado:
```
C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana\GEPCP Ferreteria El Pana\publish\GEPCP Ferreteria El Pana.exe
```

### Instalador:
```
C:\Users\soler\OneDrive - Universidad Estatal a Distancia\Documentos\GEPCP Ferreteria El Pana\Instalador\Output\Setup_FerreteriaElPana.exe
```

### Ubicación después de instalar:
```
C:\Program Files\GEPCP Ferreteria El Pana\GEPCP Ferreteria El Pana.exe
```

---

## 🎉 RESULTADO ESPERADO

**El sistema debe iniciar completamente automático:**
- Sin intervención manual
- Sin necesidad de abrir el navegador
- Sin necesidad de escribir URL
- Consola desaparece sola
- Todo listo para trabajar en 3-4 segundos

**Si alguno de estos pasos falla, reportar el problema con el mensaje exacto de error que aparece en la consola.**

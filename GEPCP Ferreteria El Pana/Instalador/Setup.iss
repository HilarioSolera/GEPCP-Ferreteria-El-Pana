; ═══════════════════════════════════════════════════════════════════
; INSTALADOR PROFESIONAL - GEPCP FERRETERÍA EL PANA
; Sistema de Gestión de Planillas y Recursos Humanos
; ═══════════════════════════════════════════════════════════════════

#define MyAppName "GEPCP Ferretería El Pana"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Ferretería El Pana"
#define MyAppURL "https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana"
#define MyAppExeName "GEPCP Ferreteria El Pana.exe"
#define MyAppServiceName "GEPCPFerreteriaElPana"
#define MyAppServiceDisplayName "GEPCP Ferretería El Pana"

[Setup]
; Identificador único de la aplicación (UUID)
AppId={{8A7F9C2E-4B5D-4E8F-9A1C-6D3E7F8B9C0A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Configuración de instalación
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes

; Archivos de salida
OutputDir=Output
OutputBaseFilename=GEPCP_FerreteriaElPana_Setup_v{#MyAppVersion}
SetupIconFile=..\wwwroot\favicon.ico

; Compresión y empaquetado
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

; Privilegios y arquitectura
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

; Iconos y apariencia
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
WizardImageFile=compiler:WizModernImage-IS.bmp
WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
spanish.WelcomeLabel1=Bienvenido al Asistente de Instalación de [name]
spanish.WelcomeLabel2=Este programa instalará [name/ver] en su equipo.%n%nSistema profesional de gestión de planillas, nóminas, aguinaldos, vacaciones y recursos humanos para Costa Rica.%n%nSe recomienda cerrar todas las aplicaciones antes de continuar.

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el &Escritorio"; GroupDescription: "Accesos directos:"
Name: "quicklaunchicon"; Description: "Crear acceso directo en la barra de &Tareas"; GroupDescription: "Accesos directos:"; Flags: unchecked
Name: "autostart"; Description: "Iniciar automáticamente con &Windows (recomendado)"; GroupDescription: "Opciones del servicio:"; Flags: checkedonce
Name: "firewall"; Description: "Configurar regla de &Firewall"; GroupDescription: "Opciones del servicio:"; Flags: checkedonce

[Files]
; Copiar todos los archivos publicados
Source: "..\bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Documentación
Source: "MANUAL_INSTALACION.txt"; DestDir: "{app}"; Flags: ignoreversion; AfterInstall: CreateManual
Source: "LEEME.txt"; DestDir: "{app}"; Flags: ignoreversion; AfterInstall: CreateReadme
; Script de inicio rápido
Source: "IniciarSistema.bat"; DestDir: "{app}"; Flags: ignoreversion; AfterInstall: CreateBatchFile

[Icons]
; Acceso directo en menú inicio
Name: "{group}\{#MyAppName}"; Filename: "{app}\IniciarSistema.bat"; IconFilename: "{app}\{#MyAppExeName}"; Comment: "Iniciar Sistema GEPCP"
Name: "{group}\Manual de Usuario"; Filename: "{app}\MANUAL_INSTALACION.txt"; IconFilename: "{sys}\shell32.dll"; IconIndex: 70
Name: "{group}\Carpeta de Instalación"; Filename: "{app}"; IconFilename: "{sys}\shell32.dll"; IconIndex: 3
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"; IconFilename: "{sys}\shell32.dll"; IconIndex: 31

; Acceso directo en escritorio
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\IniciarSistema.bat"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Comment: "Iniciar Sistema GEPCP"

; Acceso directo en barra de tareas (Quick Launch)
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\IniciarSistema.bat"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Registry]
; Iniciar servicio con Windows
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppServiceName}"; ValueData: "sc.exe start {#MyAppServiceName}"; Tasks: autostart; Flags: uninsdeletevalue

[Run]
; 1. Detener y eliminar servicio existente si existe
Filename: "sc.exe"; Parameters: "stop {#MyAppServiceName}"; Flags: runhidden waituntilterminated; StatusMsg: "Deteniendo servicio anterior..."; Check: ServiceExists('{#MyAppServiceName}')
Filename: "sc.exe"; Parameters: "delete {#MyAppServiceName}"; Flags: runhidden waituntilterminated; Check: ServiceExists('{#MyAppServiceName}')

; 2. Crear e instalar el servicio
Filename: "sc.exe"; Parameters: "create {#MyAppServiceName} binPath= ""{app}\{#MyAppExeName}"" start= auto DisplayName= ""{#MyAppServiceDisplayName}"""; Flags: runhidden waituntilterminated; StatusMsg: "Registrando servicio de Windows..."
Filename: "sc.exe"; Parameters: "description {#MyAppServiceName} ""Sistema de gestión de planillas, nóminas, aguinaldos, vacaciones y recursos humanos para Ferretería El Pana. Cumple con legislación laboral costarricense 2026."""; Flags: runhidden waituntilterminated

; 3. Configurar recuperación automática del servicio
Filename: "sc.exe"; Parameters: "failure {#MyAppServiceName} reset= 86400 actions= restart/60000/restart/60000/restart/60000"; Flags: runhidden waituntilterminated; StatusMsg: "Configurando recuperación automática..."

; 4. Configurar firewall
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""{#MyAppName}"""; Flags: runhidden; Tasks: firewall
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""{#MyAppName}"" dir=in action=allow protocol=TCP localport=5000 description=""Permite acceso al sistema GEPCP Ferretería El Pana"""; Flags: runhidden waituntilterminated; StatusMsg: "Configurando firewall de Windows..."; Tasks: firewall

; 5. Inicializar base de datos
Filename: "{app}\{#MyAppExeName}"; Parameters: "--migrate-database"; WorkingDir: "{app}"; Flags: runhidden waituntilterminated; StatusMsg: "Inicializando base de datos..."

; 6. Iniciar servicio
Filename: "sc.exe"; Parameters: "start {#MyAppServiceName}"; Flags: runhidden waituntilterminated; StatusMsg: "Iniciando servicio..."

; 7. Esperar a que el servicio esté listo
Filename: "{sys}\timeout.exe"; Parameters: "/t 5 /nobreak"; Flags: runhidden waituntilterminated; StatusMsg: "Verificando inicio del servicio..."

; 8. Abrir navegador con el sistema
Filename: "http://localhost:5000"; Flags: shellexec nowait skipifsilent; Description: "Abrir el sistema en el navegador"; StatusMsg: "Abriendo sistema..."

[UninstallRun]
; Detener y eliminar el servicio
Filename: "sc.exe"; Parameters: "stop {#MyAppServiceName}"; Flags: runhidden waituntilterminated; RunOnceId: "StopService"
Filename: "{sys}\timeout.exe"; Parameters: "/t 3 /nobreak"; Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete {#MyAppServiceName}"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteService"
; Eliminar regla de firewall
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""{#MyAppName}"""; Flags: runhidden; RunOnceId: "RemoveFirewallRule"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Logs"
Type: filesandordirs; Name: "{app}\wwwroot\uploads"

[Code]
var
  BackupDatabasePage: TInputOptionWizardPage;
  ConfigEmailPage: TInputQueryWizardPage;

// Verificar si el servicio existe
function ServiceExists(ServiceName: string): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('sc.exe', 'query ' + ServiceName, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

// Inicializar páginas personalizadas del asistente
procedure InitializeWizard;
begin
  // Página de respaldo de base de datos
  BackupDatabasePage := CreateInputOptionPage(wpSelectTasks,
	'Configuración de Base de Datos',
	'¿Qué desea hacer con la base de datos?',
	'Si ya existe una instalación previa, puede conservar los datos existentes.',
	True, False);

  BackupDatabasePage.Add('Mantener datos existentes (recomendado si actualiza)');
  BackupDatabasePage.Add('Crear nueva base de datos vacía');
  BackupDatabasePage.SelectedValueIndex := 0;

  // Página de configuración de correo
  ConfigEmailPage := CreateInputQueryPage(wpSelectTasks,
	'Configuración de Correo Electrónico (Opcional)',
	'Configure el envío de correos para boletas y reportes',
	'Puede configurar esto después editando el archivo appsettings.Production.json');

  ConfigEmailPage.Add('Correo electrónico (Gmail):', False);
  ConfigEmailPage.Add('Contraseña de aplicación:', True);
  ConfigEmailPage.Values[0] := 'ferreteriaelpana2026@gmail.com';
end;

// Verificar instalación previa antes de instalar
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  // Detener servicio existente si está corriendo
  if ServiceExists('{#MyAppServiceName}') then
  begin
	Exec('sc.exe', 'stop {#MyAppServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
	Sleep(3000);

	// Eliminar servicio existente
	Exec('sc.exe', 'delete {#MyAppServiceName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
	Sleep(1000);
  end;

  Result := '';
end;

// Acciones posteriores a la instalación
procedure CurStepChanged(CurStep: TSetupStep);
var
  DatabasePath: String;
  BackupPath: String;
  ConfigPath: String;
  ConfigContent: AnsiString;
  EmailUser: String;
  EmailPass: String;
begin
  if CurStep = ssPostInstall then
  begin
	DatabasePath := ExpandConstant('{app}\GEPCP_Ferreteria_El_Pana.db');

	// Manejo de base de datos
	if BackupDatabasePage.SelectedValueIndex = 1 then
	begin
	  // El usuario quiere crear nueva base de datos
	  if FileExists(DatabasePath) then
	  begin
		BackupPath := ExpandConstant('{userdocs}\GEPCP_Backup_') + 
					 GetDateTimeString('yyyymmdd_hhnnss', #0, #0) + '.db';
		FileCopy(DatabasePath, BackupPath, False);
		DeleteFile(DatabasePath);
		MsgBox('Base de datos anterior respaldada en:' + #13#10 + #13#10 + BackupPath + 
			   #13#10 + #13#10 + 'Se creará una nueva base de datos vacía al iniciar el sistema.', 
			   mbInformation, MB_OK);
	  end;
	end;

	// Configurar correo si se proporcionó
	EmailUser := Trim(ConfigEmailPage.Values[0]);
	EmailPass := Trim(ConfigEmailPage.Values[1]);

	if (EmailUser <> '') and (EmailPass <> '') then
	begin
	  ConfigPath := ExpandConstant('{app}\appsettings.Production.json');
	  if LoadStringFromFile(ConfigPath, ConfigContent) then
	  begin
		StringChangeEx(ConfigContent, '"Usuario": "ferreteriaelpana2026@gmail.com"', 
					  '"Usuario": "' + EmailUser + '"', True);
		StringChangeEx(ConfigContent, '"Password": ""', 
					  '"Password": "' + EmailPass + '"', True);
		SaveStringToFile(ConfigPath, ConfigContent, False);
	  end;
	end;
  end;
end;

// Mensaje de finalización personalizado
procedure CurPageChanged(CurPageID: Integer);
var
  FinalMessage: String;
begin
  if CurPageID = wpFinished then
  begin
	FinalMessage := '¡Instalación completada exitosamente!' + #13#10 + #13#10 +
					'El sistema GEPCP Ferretería El Pana está ahora instalado y funcionando.' + #13#10 + #13#10 +
					'ACCESO AL SISTEMA:' + #13#10 +
					'• URL: http://localhost:5000' + #13#10 +
					'• Acceso directo: En escritorio o menú inicio' + #13#10 + #13#10 +
					'DOCUMENTACIÓN:' + #13#10 +
					'• Manual completo en: ' + ExpandConstant('{app}\MANUAL_INSTALACION.txt') + #13#10 + #13#10 +
					'SOPORTE TÉCNICO:' + #13#10 +
					'• Email: ferreteriaelpana2026@gmail.com';

	WizardForm.FinishedLabel.Caption := FinalMessage;
  end;
end;

// Confirmación de desinstalación
function InitializeUninstall(): Boolean;
var
  DialogResult: Integer;
  DatabasePath: String;
  BackupPath: String;
begin
  DatabasePath := ExpandConstant('{app}\GEPCP_Ferreteria_El_Pana.db');

  DialogResult := MsgBox('¿Está seguro de desinstalar GEPCP Ferretería El Pana?' + #13#10 + #13#10 +
						 '¿Desea conservar la base de datos con todos los registros?' + #13#10 + #13#10 +
						 'SÍ = Conservar datos (recomendado)' + #13#10 +
						 'NO = Eliminar todo' + #13#10 +
						 'CANCELAR = Cancelar desinstalación', 
						 mbConfirmation, MB_YESNOCANCEL);

  if DialogResult = IDCANCEL then
  begin
	Result := False;
	Exit;
  end;

  if DialogResult = IDYES then
  begin
	// Crear respaldo de la base de datos
	if FileExists(DatabasePath) then
	begin
	  BackupPath := ExpandConstant('{userdocs}\GEPCP_Backup_') + 
				   GetDateTimeString('yyyymmdd_hhnnss', #0, #0) + '.db';
	  FileCopy(DatabasePath, BackupPath, False);
	  MsgBox('Base de datos respaldada en:' + #13#10 + #13#10 + BackupPath + 
			 #13#10 + #13#10 + 'Podrá restaurarla copiando este archivo a la carpeta de instalación en el futuro.', 
			 mbInformation, MB_OK);
	end;
  end;

  Result := True;
end;

// Crear archivo BAT para inicio rápido
procedure CreateBatchFile();
var
  BatContent: AnsiString;
  BatPath: String;
begin
  BatPath := ExpandConstant('{app}\IniciarSistema.bat');
  BatContent := '@echo off' + #13#10 +
				'title GEPCP Ferreteria El Pana' + #13#10 +
				'cls' + #13#10 +
				'echo.' + #13#10 +
				'echo ════════════════════════════════════════════════════════' + #13#10 +
				'echo   GEPCP Ferreteria El Pana - Iniciando Sistema' + #13#10 +
				'echo ════════════════════════════════════════════════════════' + #13#10 +
				'echo.' + #13#10 +
				'' + #13#10 +
				'REM Verificar si el servicio está corriendo' + #13#10 +
				'sc query {#MyAppServiceName} ^| find "RUNNING" >nul' + #13#10 +
				'' + #13#10 +
				'if %errorlevel% == 0 (' + #13#10 +
				'    echo [OK] El servicio esta corriendo' + #13#10 +
				'    echo.' + #13#10 +
				'    echo Abriendo navegador en http://localhost:5000' + #13#10 +
				'    start http://localhost:5000' + #13#10 +
				'    timeout /t 2 >nul' + #13#10 +
				'    exit' + #13#10 +
				')' + #13#10 +
				'' + #13#10 +
				'echo El servicio no esta activo. Iniciando...' + #13#10 +
				'net start {#MyAppServiceName}' + #13#10 +
				'' + #13#10 +
				'if %errorlevel% == 0 (' + #13#10 +
				'    echo [OK] Servicio iniciado correctamente' + #13#10 +
				'    timeout /t 3 >nul' + #13#10 +
				'    echo.' + #13#10 +
				'    echo Abriendo navegador en http://localhost:5000' + #13#10 +
				'    start http://localhost:5000' + #13#10 +
				'    timeout /t 2 >nul' + #13#10 +
				') else (' + #13#10 +
				'    echo [ERROR] No se pudo iniciar el servicio' + #13#10 +
				'    echo.' + #13#10 +
				'    echo Ejecute este archivo como Administrador o' + #13#10 +
				'    echo inicie el servicio desde Servicios de Windows' + #13#10 +
				'    echo.' + #13#10 +
				'    pause' + #13#10 +
				')' + #13#10 +
				'' + #13#10 +
				'exit';

  SaveStringToFile(BatPath, BatContent, False);
end;

// Crear manual de instalación
procedure CreateManual();
var
  ManualContent: String;
  ManualPath: String;
begin
  ManualPath := ExpandConstant('{app}\MANUAL_INSTALACION.txt');
  ManualContent := 
'═══════════════════════════════════════════════════════════════════' + #13#10 +
'  GEPCP FERRETERÍA EL PANA' + #13#10 +
'  Manual de Usuario y Administración' + #13#10 +
'  Versión 1.0.0 - Sistema de Gestión de Planillas y RRHH' + #13#10 +
'═══════════════════════════════════════════════════════════════════' + #13#10 +
'' + #13#10 +
'█ ACCESO AL SISTEMA' + #13#10 +
'─────────────────────────────────────────────────────────────────' + #13#10 +
'' + #13#10 +
'  URL LOCAL: http://localhost:5000' + #13#10 +
'' + #13#10 +
'  INICIAR SISTEMA:' + #13#10 +
'  • Doble clic en acceso directo del escritorio' + #13#10 +
'  • Doble clic en: IniciarSistema.bat' + #13#10 +
'  • El servicio inicia automáticamente con Windows' + #13#10 +
'' + #13#10 +
'█ UBICACIÓN DE ARCHIVOS' + #13#10 +
'─────────────────────────────────────────────────────────────────' + #13#10 +
'' + #13#10 +
'  Aplicación:    ' + ExpandConstant('{app}') + #13#10 +
'  Base de Datos: ' + ExpandConstant('{app}\GEPCP_Ferreteria_El_Pana.db') + #13#10 +
'  Configuración: ' + ExpandConstant('{app}\appsettings.Production.json') + #13#10 +
'  Logs:          ' + ExpandConstant('{app}\Logs\') + #13#10 +
'' + #13#10 +
'█ CONFIGURACIÓN DE CORREO ELECTRÓNICO' + #13#10 +
'─────────────────────────────────────────────────────────────────' + #13#10 +
'' + #13#10 +
'  Para enviar boletas y reportes por correo:' + #13#10 +
'' + #13#10 +
'  1. Editar: appsettings.Production.json' + #13#10 +
'  2. Buscar sección "Email"' + #13#10 +
'  3. Completar Usuario y Password' + #13#10 +
'  4. Reiniciar servicio:' + #13#10 +
'     PowerShell (Admin): Restart-Service {#MyAppServiceName}' + #13#10 +
'' + #13#10 +
'  IMPORTANTE para Gmail:' + #13#10 +
'  • Usar "Contraseña de aplicación"' + #13#10 +
'  • Generar en: https://myaccount.google.com/apppasswords' + #13#10 +
'' + #13#10 +
'█ GESTIÓN DEL SERVICIO' + #13#10 +
'─────────────────────────────────────────────────────────────────' + #13#10 +
'' + #13#10 +
'  INICIAR:   net start {#MyAppServiceName}' + #13#10 +
'  DETENER:   net stop {#MyAppServiceName}' + #13#10 +
'  REINICIAR: Restart-Service {#MyAppServiceName}' + #13#10 +
'  ESTADO:    sc query {#MyAppServiceName}' + #13#10 +
'' + #13#10 +
'  (Ejecutar PowerShell como Administrador)' + #13#10 +
'' + #13#10 +
'█ RESPALDO DE DATOS' + #13#10 +
'─────────────────────────────────────────────────────────────────' + #13#10 +
'' + #13#10 +
'  RECOMENDADO: Respaldo semanal' + #13#10 +
'' + #13#10 +
'  1. Detener servicio: net stop {#MyAppServiceName}' + #13#10 +
'  2. Copiar archivo: GEPCP_Ferreteria_El_Pana.db' + #13#10 +
'  3. Guardar en ubicación segura (USB, nube)' + #13#10 +
'  4. Reiniciar servicio: net start {#MyAppServiceName}' + #13#10 +
'' + #13#10 +
'█ SOPORTE TÉCNICO' + #13#10 +
'─────────────────────────────────────────────────────────────────' + #13#10 +
'' + #13#10 +
'  Email:  ferreteriaelpana2026@gmail.com' + #13#10 +
'  GitHub: https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana' + #13#10 +
'' + #13#10 +
'═══════════════════════════════════════════════════════════════════' + #13#10 +
'  © 2026 GEPCP Ferretería El Pana' + #13#10 +
'═══════════════════════════════════════════════════════════════════';

  SaveStringToFile(ManualPath, ManualContent, False);
end;

// Crear archivo LEEME
procedure CreateReadme();
var
  ReadmeContent: String;
  ReadmePath: String;
begin
  ReadmePath := ExpandConstant('{app}\LEEME.txt');
  ReadmeContent := 
'═══════════════════════════════════════════════════════════════════' + #13#10 +
'  GEPCP FERRETERÍA EL PANA - SISTEMA INSTALADO' + #13#10 +
'═══════════════════════════════════════════════════════════════════' + #13#10 +
'' + #13#10 +
'¡Gracias por instalar GEPCP Ferretería El Pana!' + #13#10 +
'' + #13#10 +
'ACCESO RÁPIDO:' + #13#10 +
'  → Abrir navegador: http://localhost:5000' + #13#10 +
'  → Doble clic en: IniciarSistema.bat' + #13#10 +
'' + #13#10 +
'DOCUMENTACIÓN:' + #13#10 +
'  → Ver: MANUAL_INSTALACION.txt' + #13#10 +
'' + #13#10 +
'SOPORTE:' + #13#10 +
'  → ferreteriaelpana2026@gmail.com' + #13#10 +
'' + #13#10 +
'═══════════════════════════════════════════════════════════════════';

  SaveStringToFile(ReadmePath, ReadmeContent, False);
end;

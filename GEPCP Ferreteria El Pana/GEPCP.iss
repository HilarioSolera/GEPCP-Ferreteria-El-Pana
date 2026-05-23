#define MyAppName "GEPCP Ferretería El Pana"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Hilario Solera"
#define MyAppURL "https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana"
#define MyAppExeName "GEPCP Ferreteria El Pana.exe"
#define PublishPath AddBackslash(SourcePath) + "publish\"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\GEPCP Ferreteria El Pana
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir={#SourcePath}InstaladorOutput
OutputBaseFilename=GEPCP_Instalador_v1.0.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
SetupIconFile={#SourcePath}logo-el-pana-valid.ico
UninstallDisplayIcon={app}\logo-el-pana-valid.ico
CloseApplications=yes
RestartApplications=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el Escritorio"; GroupDescription: "Accesos directos:"
Name: "startmenuicon"; Description: "Crear acceso directo en el Menú Inicio"; GroupDescription: "Accesos directos:"

[Files]
Source: "{#PublishPath}{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishPath}appsettings.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#PublishPath}appsettings.Production.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourcePath}logo-el-pana-valid.ico"; DestDir: "{app}"; DestName: "logo-el-pana-valid.ico"; Flags: ignoreversion
Source: "{#PublishPath}wwwroot\favicon.ico"; DestDir: "{app}"; DestName: "favicon.ico"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#PublishPath}wwwroot\favicon_new.ico"; DestDir: "{app}"; DestName: "favicon_new.ico"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourcePath}IniciarSistema.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishPath}*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{sys}\wscript.exe"; Parameters: """{app}\IniciarSistemaHidden.vbs"""; WorkingDir: "{app}"; IconFilename: "{app}\logo-el-pana-valid.ico"; Tasks: startmenuicon
Name: "{userdesktop}\{#MyAppName}"; Filename: "{sys}\wscript.exe"; Parameters: """{app}\IniciarSistemaHidden.vbs"""; WorkingDir: "{app}"; IconFilename: "{app}\logo-el-pana-valid.ico"; Tasks: desktopicon
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{sys}\wscript.exe"; Parameters: """{app}\IniciarSistemaHidden.vbs"""; Description: "Iniciar {#MyAppName} ahora"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "curl"; Parameters: "-s -X POST http://localhost:5002/api/server/shutdown"; Flags: runhidden skipifdoesntexist
Filename: "cmd.exe"; Parameters: "/c timeout /t 2 /nobreak"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\GEPCP_FerreteriaElPana"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  VbsPath: string;
  Lines: TArrayOfString;
begin
  if CurStep = ssPostInstall then
  begin
    VbsPath := ExpandConstant('{app}\IniciarSistemaHidden.vbs');
    SetArrayLength(Lines, 5);
    Lines[0] := 'Set WshShell = CreateObject("WScript.Shell")';
    Lines[1] := 'Set FSO = CreateObject("Scripting.FileSystemObject")';
    Lines[2] := 'BatPath = FSO.GetParentFolderName(WScript.ScriptFullName) & "\\IniciarSistema.bat"';
    Lines[3] := 'WshShell.CurrentDirectory = FSO.GetParentFolderName(WScript.ScriptFullName)';
    Lines[4] := 'WshShell.Run Chr(34) & BatPath & Chr(34), 0, False';
    SaveStringsToFile(VbsPath, Lines, False);
  end;
end;

procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel2.Caption :=
    'Este asistente instalará GEPCP - Sistema de Gestión de Empleados, Préstamos, Comisiones y Planilla para Ferretería El Pana S.R.L.' + #13#10 + #13#10 +
    'El sistema se ejecuta localmente en su computadora y no requiere conexión a internet para su funcionamiento.' + #13#10 + #13#10 +
    'Se recomienda cerrar todas las aplicaciones antes de continuar.';
end;

[Setup]
AppName=GEPCP Ferreteria El Pana
AppVersion=1.0
AppPublisher=GEPCP
DefaultDirName={pf}\GEPCP Ferreteria El Pana
DefaultGroupName=GEPCP Ferreteria El Pana
OutputDir=Output
OutputBaseFilename=GEPCP-Ferreteria-El-Pana-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=admin

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear icono en el escritorio"; GroupDescription: "Iconos adicionales:"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0\GEPCP Ferreteria El Pana.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
Source: "wwwroot\*"; DestDir: "{app}\wwwroot"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "IniciarSistema.bat"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\GEPCP Ferreteria El Pana"; Filename: "{app}\IniciarSistema.bat"; WorkingDir: "{app}"
Name: "{group}\Desinstalar GEPCP"; Filename: "{uninstallexe}"
Name: "{commondesktop}\GEPCP Ferreteria El Pana"; Filename: "{app}\IniciarSistema.bat"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\IniciarSistema.bat"; Description: "Iniciar GEPCP"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}"
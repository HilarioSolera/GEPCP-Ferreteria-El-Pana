[Setup]
AppName=GEPCP Ferreteria El Pana
AppVersion=1.0
AppPublisher=GEPCP
AppPublisherURL=https://github.com/HilarioSolera/GEPCP-Ferreteria-El-Pana
DefaultDirName={pf}\GEPCP Ferreteria El Pana
DefaultGroupName=GEPCP Ferreteria El Pana
OutputDir=Output
OutputBaseFilename=GEPCP-Ferreteria-El-Pana-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
PrivilegesRequired=admin
UninstallDisplayIcon={app}\GEPCP Ferreteria El Pana.exe

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0\GEPCP Ferreteria El Pana.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
Source: "wwwroot\*"
Source: "IniciarSistema.bat"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\GEPCP Ferreteria El Pana"; Filename: "{app}\IniciarSistema.bat"; WorkingDir: "{app}"; Comment: "Iniciar Sistema GEPCP"
Name: "{group}\Desinstalar GEPCP"; Filename: "{uninstallexe}"
Name: "{commondesktop}\GEPCP Ferreteria El Pana"; Filename: "{app}\IniciarSistema.bat"; WorkingDir: "{app}"; Comment: "Iniciar Sistema GEPCP"; Tasks: desktopicon

[Run]
Filename: "{app}\IniciarSistema.bat"; Description: "Iniciar GEPCP"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}"

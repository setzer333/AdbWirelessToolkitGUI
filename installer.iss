; =============================================================================
; Inno Setup Script for AdbWirelessToolkitGUI
; Version 1.0 - Dual License (GPLv3 + MIT)
; =============================================================================

#define MyAppName "AdbWirelessToolkitGUI"
#define MyAppVersion "1.0"
#define MyAppPublisher "Setzer333"
#define MyAppPublisherURL "https://github.com/Setzer333"
#define MyAppExeName "AdbWirelessToolkitGUI.exe"
#define SourceDir "..\publish\win-x64"

[Setup]
AppId={{5B37878B-7880-4B20-A019-8859E176074D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppPublisherURL}
AppSupportURL={#MyAppPublisherURL}
AppUpdatesURL={#MyAppPublisherURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename={#MyAppExeName}_Setup_v{#MyAppVersion}_x64
SetupIconFile=..\assets\Android-Logo-2008.ico
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
DisableProgramGroupPage=yes
AllowNoIcons=yes
CreateAppDir=yes
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousLanguage=yes
UsePreviousTasks=yes

LicenseFile=..\Combined-License_2.txt

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenuicon"; Description: "Crear acceso directo en el Menú de Inicio"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\PlatformTools\*"; DestDir: "{app}\PlatformTools"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceDir}\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceDir}\Languages\*"; DestDir: "{app}\Languages"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "..\Combined-License_2.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Gestión inalámbrica de dispositivos Android vía ADB"; IconFilename: "{app}\Assets\Android-Logo-2008.ico"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Gestión inalámbrica de dispositivos Android vía ADB"; IconFilename: "{app}\Assets\Android-Logo-2008.ico"; Tasks: desktopicon
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Gestión inalámbrica de dispositivos Android vía ADB"; IconFilename: "{app}\Assets\Android-Logo-2008.ico"; Tasks: startmenuicon
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[InstallDelete]
Type: files; Name: "{app}\*.tmp"
; =============================================================================
; installer.iss - Inno Setup Script para AdbWirelessToolkitGUI
; =============================================================================
; Compilar con: iscc.exe installer.iss
; Requiere: Inno Setup 6.2+ (https://jrsoftware.org/isinfo.php)
; Ejecutar después de: make publish-x64
; =============================================================================

#define MyAppName "AdbWirelessToolkitGUI"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AdbWirelessToolkitGUI Contributors"
#define MyAppURL "https://github.com/AdbWirelessToolkitGUI"
#define MyAppExeName "AdbWirelessToolkitGUI.exe"
#define SourceDir "publish\win-x64"

[Setup]
; --- Información básica ---
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename={#MyAppName}_Setup_v{#MyAppVersion}_x64
SetupIconFile={#SourceDir}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64

; --- Licencia Dual (MIT / GPLv3) ---
LicenseFile=Combined-License.txt
; Obliga a aceptar la licencia antes de continuar
LicenseAccepted=1

; --- Configuración de instalación ---
DisableDirPage=no
DisableProgramGroupPage=no
AllowNoIcons=yes
CreateAppDir=yes
AlwaysShowGroupOnReadyPage=yes
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousLanguage=yes
UsePreviousTasks=yes

; --- Internacionalización ---
LanguageName=Spanish
LanguageFile=compiler:Languages\Spanish.isl

; --- Ventana ---
WindowResizable=yes
WindowMinWidth=500
WindowMinHeight=400
WindowShowCaption=yes
WindowStartMaximized=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
; --- Ejecutable principal ---
Source: "{#SourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; --- Carpeta PlatformTools completa (binarios ADB) ---
Source: "{#SourceDir}\PlatformTools\*"; DestDir: "{app}\PlatformTools"; Flags: ignoreversion recursesubdirs createallsubdirs

; --- Documentación y licencias ---
Source: "LICENSE-MIT.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE-GPL3.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "Combined-License.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; --- Accesos directos ---
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Gestión inalámbrica de dispositivos Android vía ADB"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; WorkingDir: "{app}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Gestión inalámbrica de dispositivos Android vía ADB"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: quicklaunchicon

[Run]
; --- Opción para ejecutar al finalizar instalación ---
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; --- Limpieza completa al desinstalar ---
Type: filesandordirs; Name: "{app}"

[Registry]
; --- Asociación de archivo .apk opcional (requiere elevación) ---
; Root: HKCR; Subkey: ".apk"; ValueType: string; ValueData: "AdbWirelessToolkitGUI.APK"; Flags: uninsdeletekey
; Root: HKCR; Subkey: "AdbWirelessToolkitGUI.APK"; ValueType: string; ValueData: "Android Package (ADB Toolkit)"; Flags: uninsdeletekey
; Root: HKCR; Subkey: "AdbWirelessToolkitGUI.APK\shell\open\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey

[InstallDelete]
; --- Limpiar instalaciones previas corruptas ---
Type: files; Name: "{app}\*.tmp"

[Code]
; --- Personalización de la página de licencia ---
procedure InitializeWizard();
begin
  // Ajustar tamaño de la ventana del instalador
  WizardForm.Width := 600;
  WizardForm.Height := 480;
  
  // Hacer el texto de licencia más legible
  with WizardForm.LicenseMemo do
  begin
    Font.Name := 'Consolas';
    Font.Size := 9;
    ScrollBars := ssBoth;
    WordWrap := False;
  end;
end;

; --- Verificar que PlatformTools existe antes de instalar ---
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then
  begin
    if not DirExists(ExpandConstant('{#SourceDir}\PlatformTools')) then
    begin
      MsgBox('ERROR: No se encuentra la carpeta PlatformTools en el directorio de publicación.' + #13#10 +
             'Ejecute "make publish-x64" antes de compilar el instalador.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

; --- Verificar .NET Runtime (opcional, solo informativo) ---
procedure CurStepChanged(CurStep: TSetupStep);
var
  NetVersion: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Verificar si .NET 8 Runtime está instalado (solo informativo)
    if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Version', NetVersion) then
    begin
      Log('.NET Framework detectado: ' + NetVersion);
    end;
  end;
end;

; --- Validación de arquitectura ---
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Verificar que estamos en Windows 10/11 x64
  if not IsWin64 then
  begin
    MsgBox('Este instalador requiere Windows de 64 bits (x64).', mbError, MB_OK);
    Result := False;
  end;
  
  // Verificar versión mínima Windows 10 (10.0.10240)
  if (GetWindowsVersion < $0A000000) then // Windows 10 = 10.0
  begin
    MsgBox('Se requiere Windows 10 o superior.', mbError, MB_OK);
    Result := False;
  end;
end;

; =============================================================================
; NOTAS DE COMPILACIÓN
; =============================================================================
; 1. Ejecutar primero: make publish-x64
; 2. Luego compilar:   iscc.exe installer.iss
; 3. Salida:           Output/AdbWirelessToolkitGUI_Setup_v1.0.0_x64.exe
;
; El instalador incluye:
; - Ejecutable Self-Contained Single-File (sin dependencias .NET externas)
; - Carpeta PlatformTools completa (adb.exe, AdbWinApi.dll, AdbWinUsbApi.dll, fastboot.exe, etc.)
; - Licencias MIT, GPLv3 y Combined-License.txt
; - Pantalla de licencia obligatoria (LicenseAccepted=0)
; - Accesos directos en Menú Inicio, Escritorio (opcional) y Quick Launch (opcional)
; - Desinstalador limpio
; =============================================================================
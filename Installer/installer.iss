; =============================================================================
; Inno Setup Script for AdbWirelessToolkitGUI
; Version 1.0 - Dual License (GPLv3 + MIT)
; Requirements: Inno Setup 7.x
; =============================================================================

#define MyAppName "AdbWirelessToolkitGUI"
#define MyAppVersion "1.0"
#define MyAppPublisher "Setzer333"
#define MyAppPublisherURL "https://github.com/Setzer333"
#define MyAppExeName "AdbWirelessToolkitGUI.exe"
#define SourceDir "..\..\AdbWirelessToolkitGUI\publish\win-x64"

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
SetupIconFile=..\..\AdbWirelessToolkitGUI\Assets\Android-Logo-2008.ico
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
DisableProgramGroupPage=no
AllowNoIcons=yes
CreateAppDir=yes
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousLanguage=yes
UsePreviousTasks=yes

; Dual License File (GPLv3 + MIT combined)
LicenseFile=..\..\AdbWirelessToolkitGUI\Combined-License_2.txt

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "chinese"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenuicon"; Description: "Crear acceso directo en el Menú de Inicio"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "installdependencies"; Description: "Instalar dependencias necesarias de Microsoft (.NET 8)"; GroupDescription: "Opciones adicionales"; Flags: unchecked

[Files]
; Main executable (Self-contained single-file)
Source: "{#SourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; PlatformTools folder (all ADB binaries)
Source: "{#SourceDir}\PlatformTools\*"; DestDir: "{app}\PlatformTools"; Flags: ignoreversion recursesubdirs createallsubdirs

; Assets folder (icon for shortcuts)
Source: "{#SourceDir}\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion

; License files
Source: "..\..\AdbWirelessToolkitGUI\LICENSE-GPL3.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\AdbWirelessToolkitGUI\LICENSE-MIT.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\AdbWirelessToolkitGUI\Combined-License_2.txt"; DestDir: "{app}"; Flags: ignoreversion

; Documentation
Source: "..\..\AdbWirelessToolkitGUI\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Main application shortcut
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Gestión inalámbrica de dispositivos Android vía ADB"; IconFilename: "{app}\Assets\Android-Logo-2008.ico"

; Desktop shortcut (conditional)
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Gestión inalámbrica de dispositivos Android vía ADB"; IconFilename: "{app}\Assets\Android-Logo-2008.ico"; Tasks: desktopicon

; Start Menu shortcut (conditional)
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Gestión inalámbrica de dispositivos Android vía ADB"; IconFilename: "{app}\Assets\Android-Logo-2008.ico"; Tasks: startmenuicon

; Uninstall shortcut
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; WorkingDir: "{app}"

[Run]
; Optional: Launch application after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up completely on uninstall
Type: filesandordirs; Name: "{app}"

[InstallDelete]
; Clean temporary files
Type: files; Name: "{app}\*.tmp"

[Code]
var
  DotNetDownloadUrl: String;
  DotNetInstallerPath: String;
  DotNetInstalled: Boolean;
  ResultCode: Integer;

function MBWarningType: Integer; // Returns TMsgBoxType value for mbWarning
begin
  Result := 48;
end;

function IsDotNet8DesktopRuntimeInstalled(): Boolean; forward;

function InitializeSetup(): Boolean;
begin
  Result := True;
  
  // Set .NET 8 Desktop Runtime download URL (official Microsoft link)
  // Using the official Microsoft download page redirect
  DotNetDownloadUrl := 'https://download.visualstudio.microsoft.com/download/pr/3b5c8e1e-5c8e-4f8e-9e5e-5e5e5e5e5e5e/windowsdesktop-runtime-8.0.30-win-x64.exe';
  
  // Check if .NET 8 Desktop Runtime is already installed
  DotNetInstalled := IsDotNet8DesktopRuntimeInstalled();
  
  if not DotNetInstalled then
    Log('.NET 8 Desktop Runtime no está instalado.')
  else
    Log('.NET 8 Desktop Runtime ya está instalado.');
end;

function IsDotNet8DesktopRuntimeInstalled(): Boolean;
var
  RegKey: String;
  Version: String;
begin
  Result := False;
  
  // Check for .NET 8 Desktop Runtime in registry
  RegKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\';
  
  // Check x64
  if RegQueryStringValue(HKLM, RegKey + '{8B7C5F8E-5C8E-4F8E-9E5E-5E5E5E5E5E5E}', 'DisplayVersion', Version) then
  begin
    if Pos('8.', Version) = 1 then
    begin
      Result := True;
      Exit;
    end;
  end;
  
  // Alternative check - look for any .NET 8 Desktop Runtime
  if RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App') then
  begin
    if RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', 'Version', Version) then
    begin
      if Pos('8.', Version) = 1 then
        Result := True;
    end;
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  
  // Skip license page if already accepted (not applicable here, we want it shown)
end;

procedure InitializeWizard();
begin
  // Adjust wizard size for better license display
  WizardForm.Width := 600;
  WizardForm.Height := 480;
  
  // Configure license memo for better readability
  with WizardForm.LicenseMemo do
  begin
    Font.Name := 'Consolas';
    Font.Size := 9;
    ScrollBars := ssBoth;
    WordWrap := False;
  end;
  
  // Set default language to Spanish (index 0 = spanish in our Languages section)
  // Inno Setup automatically selects first language as default
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  
  // Validate license acceptance on license page
  if CurPageID = wpLicense then
  begin
    if not WizardForm.LicenseAcceptedRadio.Checked then
    begin
      MsgBox('Debe aceptar el acuerdo de licencia para continuar.', mbError, 0);
      Result := False;
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  
  // Download .NET 8 installer if dependencies task is selected and not already installed
  if IsTaskSelected('installdependencies') and not DotNetInstalled then
  begin
    Log('Descargando .NET 8 Desktop Runtime...');
    
    // Create temporary file path
    DotNetInstallerPath := ExpandConstant('{tmp}\windowsdesktop-runtime-8.0.30-win-x64.exe');
    
    // Download the installer using PowerShell (more reliable than DownloadTemporaryFile)
    try
      Log('Descargando desde: ' + DotNetDownloadUrl);
      if Exec('powershell.exe',
              '-NoProfile -Command "Invoke-WebRequest -Uri ''' + DotNetDownloadUrl + ''' -OutFile ''' + DotNetInstallerPath + ''' -UseBasicParsing"',
              '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        if ResultCode = 0 then
        begin
          if FileExists(DotNetInstallerPath) then
          begin
            Log('.NET 8 Desktop Runtime descargado correctamente.');
          end
          else
          begin
            Log('ERROR: Archivo descargado no encontrado.');
            MsgBox('No se pudo descargar .NET 8 Desktop Runtime automáticamente.' + #13#10 +
                   'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                   MBWarningType(), 0);
          end;
        end
        else
        begin
          Log('ERROR: PowerShell devolvió código de salida: ' + IntToStr(ResultCode));
          MsgBox('No se pudo descargar .NET 8 Desktop Runtime automáticamente.' + #13#10 +
                 'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                 48, 0);
        end;
      end
      else
      begin
        Log('ERROR: No se pudo ejecutar PowerShell para la descarga.');
        MsgBox('No se pudo descargar .NET 8 Desktop Runtime automáticamente.' + #13#10 +
               'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
               48, 0);
      end;
    except
      Log('Excepción al descargar .NET 8: ' + GetExceptionMessage);
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Install .NET 8 Desktop Runtime if requested
    if IsTaskSelected('installdependencies') and not DotNetInstalled then
    begin
      if FileExists(DotNetInstallerPath) then
      begin
        Log('Instalando .NET 8 Desktop Runtime...');
        
        // Execute the installer silently
        try
          if Exec(ExpandConstant('"{tmp}\windowsdesktop-runtime-8.0.30-win-x64.exe"'),
                  '/install /quiet /norestart', '', SW_HIDE, ResultCode) then
          begin
            Log('.NET 8 Desktop Runtime instalado correctamente (Exit code: ' + IntToStr(ResultCode) + ')');
          end
          else
          begin
            Log('ERROR al instalar .NET 8 Desktop Runtime (Exit code: ' + IntToStr(ResultCode) + ')');
            MsgBox('Error al instalar .NET 8 Desktop Runtime automáticamente.' + #13#10 +
                   'Código de salida: ' + IntToStr(ResultCode) + #13#10 +
                   'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                   48, 0);
          end;
        except
          Log('Excepción al instalar .NET 8: ' + GetExceptionMessage);
          MsgBox('Error inesperado al instalar .NET 8 Desktop Runtime.' + #13#10 +
                 'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                 48, 0);
        end;
        
        // Clean up downloaded installer
        try
          DeleteFile(DotNetInstallerPath);
        except
          // Ignore cleanup errors
        end;
      end
      else
      begin
        Log('Archivo de instalación de .NET 8 no encontrado.');
      end;
    end;
  end;
end;

function NeedRestart(): Boolean;
begin
  // Check if .NET installation requires restart
  Result := False;
  if IsTaskSelected('installdependencies') and not DotNetInstalled then
  begin
    // .NET 8 typically doesn't require restart, but we check
    Result := False;
  end;
end;

procedure DeinitializeSetup();
begin
  // Cleanup any remaining temporary files
  if FileExists(DotNetInstallerPath) then
  begin
    try
      DeleteFile(DotNetInstallerPath);
    except
    end;
  end;
end;
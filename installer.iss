; =============================================================================
; Inno Setup Script for AdbWirelessToolkitGUI
; Version 1.0 - Dual License (GPLv3 + MIT)
; Requirements: Inno Setup 7.x
; =============================================================================

#define MyAppName "AdbWirelessToolkitGUI"
#define MyAppVersion "1.0.0"
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
OutputBaseFilename=AdbWirelessToolkitGUI_Setup_x64
SetupIconFile=..\..\AdbWirelessToolkitGUI\assets\Android-Logo-2008.ico
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64compatible
DisableProgramGroupPage=yes
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

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenuicon"; Description: "Crear acceso directo en el Menú de Inicio"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "vcpp_x86"; Description: "Instalar Visual C++ Redistributable (x86)"; GroupDescription: "Visual C++ Redistributable (Incluido offline)"; Flags: unchecked
Name: "vcpp_x64"; Description: "Instalar Visual C++ Redistributable (x64)"; GroupDescription: "Visual C++ Redistributable (Opcional)"; Flags: unchecked
Name: "dotnet_x86"; Description: "Instalar .NET 8.0.30 Desktop Runtime (x86)"; GroupDescription: ".NET 8.0.30 Desktop Runtime (Descarga online)"; Flags: unchecked
Name: "dotnet_x64"; Description: "Instalar .NET 8.0.30 Desktop Runtime (x64)"; GroupDescription: ".NET 8.0.30 Desktop Runtime (Descarga online)"; Flags: unchecked

[Files]
; Main executable (Self-contained single-file)
Source: "{#SourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; PlatformTools folder (all ADB binaries)
Source: "{#SourceDir}\PlatformTools\*"; DestDir: "{app}\PlatformTools"; Flags: ignoreversion recursesubdirs createallsubdirs

; Assets folder (icon for shortcuts)
Source: "{#SourceDir}\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion

; RUNTIME folder (VC++ redistributables and .NET runtime URL files)
Source: "{#SourceDir}\RUNTIME\*"; DestDir: "{app}\RUNTIME"; Flags: ignoreversion recursesubdirs createallsubdirs

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
  DotNetDownloadUrlX86: String;
  DotNetInstallerPath: String;
  DotNetInstallerPathX86: String;
  DotNetInstalled: Boolean;
  ResultCode: Integer;
  VCppX86Path: String;
  VCppX64Path: String;
  DotNetX86Selected: Boolean;
  DotNetX64Selected: Boolean;

function IsDotNet8DesktopRuntimeInstalled(): Boolean; forward;

function InitializeSetup(): Boolean;
begin
  Result := True;
  
  // Determine architecture and set the appropriate .NET download URLs
  // We always use x64 for the installer since ArchitecturesAllowed=x64
  DotNetDownloadUrl := 'https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.30/windowsdesktop-runtime-8.0.30-win-x64.exe';
  DotNetDownloadUrlX86 := 'https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.30/windowsdesktop-runtime-8.0.30-win-x86.exe';
  
  // Read URLs from text files if they exist
  if FileExists(ExpandConstant('{src}\RUNTIME\windowsdesktop-runtime-8.0.30-win-x64.txt')) then
  begin
    LoadStringFromFile(ExpandConstant('{src}\RUNTIME\windowsdesktop-runtime-8.0.30-win-x64.txt'), DotNetDownloadUrl);
  end;
  
  if FileExists(ExpandConstant('{src}\RUNTIME\windowsdesktop-runtime-8.0.30-win-x86.txt')) then
  begin
    LoadStringFromFile(ExpandConstant('{src}\RUNTIME\windowsdesktop-runtime-8.0.30-win-x86.txt'), DotNetDownloadUrlX86);
  end;
  
  // Check if .NET 8 Desktop Runtime is already installed
  DotNetInstalled := IsDotNet8DesktopRuntimeInstalled();
  
  if not DotNetInstalled then
    Log('.NET 8 Desktop Runtime no está instalado.')
  else
    Log('.NET 8 Desktop Runtime ya está instalado.');
  
  // Set paths to VC++ redistributables (included in the installer)
  VCppX86Path := ExpandConstant('{app}\RUNTIME\VC_redist.x86.exe');
  VCppX64Path := ExpandConstant('{app}\RUNTIME\VC_redist.x64.exe');
  
  Log('RUTA VC++ x86: ' + VCppX86Path);
  Log('RUTA VC++ x64: ' + VCppX64Path);
  
  // Read user selections for .NET runtimes
  DotNetX86Selected := WizardIsTaskSelected('dotnet_x86');
  DotNetX64Selected := WizardIsTaskSelected('dotnet_x64');
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
      MsgBox('Debe aceptar el acuerdo de licencia para continuar.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  
  // Download .NET 8 installer if not already installed (mandatory dependency)
  if not DotNetInstalled then
  begin
    Log('Descargando .NET 8 Desktop Runtime (obligatorio)...');
    
    // Create temporary file path
    DotNetInstallerPath := ExpandConstant('{tmp}\windowsdesktop-runtime-8.0.30-win-x64.exe');
    
    // Download the installer using PowerShell (more reliable than DownloadTemporaryFile)
    try
      Log('Descargando .NET 8 Desktop Runtime desde: ' + DotNetDownloadUrl);
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
                   mbError, MB_OK);
          end;
        end
        else
        begin
          Log('ERROR: PowerShell devolvió código de salida: ' + IntToStr(ResultCode));
          MsgBox('No se pudo descargar .NET 8 Desktop Runtime automáticamente.' + #13#10 +
                 'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                 mbError, MB_OK);
        end;
      end
      else
      begin
        Log('ERROR: No se pudo ejecutar PowerShell para la descarga.');
        MsgBox('No se pudo descargar .NET 8 Desktop Runtime automáticamente.' + #13#10 +
               'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
               mbError, MB_OK);
      end;
    except
      Log('Excepción al descargar .NET 8: ' + GetExceptionMessage);
    end;
  end;
  
  // Open .NET x86 URL in browser if selected
  if DotNetX86Selected then
  begin
    Log('Abriendo URL de descarga para .NET 8.0.30 Desktop Runtime (x86)...');
    Exec('cmd.exe', '/c start "" "' + DotNetDownloadUrlX86 + '"', '', SW_HIDE, ewNoWait, ResultCode);
    Sleep(500); // Small delay to allow browser to open
  end;
  
  // Open .NET x64 URL in browser if selected
  if DotNetX64Selected then
  begin
    Log('Abriendo URL de descarga para .NET 8.0.30 Desktop Runtime (x64)...');
    Exec('cmd.exe', '/c start "" "' + DotNetDownloadUrl + '"', '', SW_HIDE, ewNoWait, ResultCode);
    Sleep(500); // Small delay to allow browser to open
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 1. Install .NET 8 Desktop Runtime x64 if not already installed (MANDATORY)
    if not DotNetInstalled then
    begin
      if FileExists(DotNetInstallerPath) then
      begin
        Log('Instalando .NET 8 Desktop Runtime x64 (obligatorio)...');
        
        // Execute the installer silently
        try
          if Exec(ExpandConstant('{tmp}\windowsdesktop-runtime-8.0.30-win-x64.exe'), '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
          begin
            Log('.NET 8 Desktop Runtime x64 instalado correctamente (Exit code: ' + IntToStr(ResultCode) + ')');
          end
          else
          begin
            Log('ERROR al instalar .NET 8 Desktop Runtime (Exit code: ' + IntToStr(ResultCode) + ')');
            MsgBox('Error al instalar .NET 8 Desktop Runtime automáticamente.' + #13#10 +
                   'Código de salida: ' + IntToStr(ResultCode) + #13#10 +
                   'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                   mbError, MB_OK);
          end;
        except
          Log('Excepción al instalar .NET 8: ' + GetExceptionMessage);
          MsgBox('Error inesperado al instalar .NET 8 Desktop Runtime.' + #13#10 +
                 'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                 mbError, MB_OK);
        end;
        
        // Clean up downloaded installer
        try
          DeleteFile(DotNetInstallerPath);
        except
        end;
      end
      else
      begin
        Log('ERROR: Archivo de instalación de .NET 8 no encontrado.');
        MsgBox('No se encontró el instalador de .NET 8. Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
               mbError, MB_OK);
      end;
    end;
    
    // 2. Install .NET 8 Desktop Runtime x86 if selected
    if DotNetX86Selected then
    begin
      DotNetInstallerPathX86 := ExpandConstant('{tmp}\windowsdesktop-runtime-8.0.30-win-x86.exe');
      
      // Download the x86 installer
      Log('Descargando .NET 8 Desktop Runtime x86...');
      try
        Log('Descargando desde: ' + DotNetDownloadUrlX86);
        if Exec('powershell.exe',
                '-NoProfile -Command "Invoke-WebRequest -Uri ''' + DotNetDownloadUrlX86 + ''' -OutFile ''' + DotNetInstallerPathX86 + ''' -UseBasicParsing"',
                '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        begin
          if ResultCode = 0 then
          begin
            if FileExists(DotNetInstallerPathX86) then
            begin
              Log('.NET 8 Desktop Runtime x86 descargado correctamente.');
            end
            else
            begin
              Log('ERROR: Archivo descargado no encontrado.');
              MsgBox('No se pudo descargar .NET 8 Desktop Runtime x86 automáticamente.' + #13#10 +
                     'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                     mbError, MB_OK);
            end;
          end
          else
          begin
            Log('ERROR: PowerShell devolvió código de salida: ' + IntToStr(ResultCode));
            MsgBox('No se pudo descargar .NET 8 Desktop Runtime x86 automáticamente.' + #13#10 +
                   'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                   mbError, MB_OK);
          end;
        end
        else
        begin
          Log('ERROR: No se pudo ejecutar PowerShell para la descarga.');
          MsgBox('No se pudo descargar .NET 8 Desktop Runtime x86 automáticamente.' + #13#10 +
                 'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                 mbError, MB_OK);
        end;
      except
        Log('Excepción al descargar .NET 8 x86: ' + GetExceptionMessage);
      end;
      
      if FileExists(DotNetInstallerPathX86) then
      begin
        Log('Instalando .NET 8 Desktop Runtime x86...');
        try
          if Exec(ExpandConstant('{tmp}\windowsdesktop-runtime-8.0.30-win-x86.exe'), '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
          begin
            Log('.NET 8 Desktop Runtime x86 instalado correctamente (Exit code: ' + IntToStr(ResultCode) + ')');
          end
          else
          begin
            Log('ERROR al instalar .NET 8 Desktop Runtime x86 (Exit code: ' + IntToStr(ResultCode) + ')');
            MsgBox('Error al instalar .NET 8 Desktop Runtime x86 automáticamente.' + #13#10 +
                   'Código de salida: ' + IntToStr(ResultCode) + #13#10 +
                   'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                   mbError, MB_OK);
          end;
        except
          Log('Excepción al instalar .NET 8 x86: ' + GetExceptionMessage);
          MsgBox('Error inesperado al instalar .NET 8 Desktop Runtime x86.' + #13#10 +
                 'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
                 mbError, MB_OK);
        end;
        
        // Clean up downloaded installer
        try
          DeleteFile(DotNetInstallerPathX86);
        except
        end;
      end
      else
      begin
        Log('ERROR: Archivo de instalación de .NET 8 x86 no encontrado.');
        MsgBox('No se pudo descargar .NET 8 Desktop Runtime x86 automáticamente.' + #13#10 +
               'Por favor, instálelo manualmente desde https://dotnet.microsoft.com/download/dotnet/8.0',
               mbError, MB_OK);
      end;
    end;
    
    // 3. .NET 8 Desktop Runtime x64 if selected (in addition to mandatory one)
    if DotNetX64Selected and not DotNetInstalled then
    begin
      // Already handled by mandatory installation above
      Log('.NET 8 Desktop Runtime x64 ya instalado como dependencia obligatoria.');
    end
    else if DotNetX64Selected and DotNetInstalled then
    begin
      Log('.NET 8 Desktop Runtime x64 ya está instalado en el sistema.');
    end;
    
    // 2. Install VC++ Redistributable x86 if selected
    if WizardIsTaskSelected('vcpp_x86') then
    begin
      if FileExists(VCppX86Path) then
      begin
        Log('Instalando Visual C++ Redistributable x86...');
        try
          if Exec(VCppX86Path, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
          begin
            Log('Visual C++ Redistributable x86 instalado correctamente (Exit code: ' + IntToStr(ResultCode) + ')');
          end
          else
          begin
            Log('ERROR al instalar Visual C++ Redistributable x86 (Exit code: ' + IntToStr(ResultCode) + ')');
            MsgBox('Error al instalar Visual C++ Redistributable x86 automáticamente.' + #13#10 +
                   'Código de salida: ' + IntToStr(ResultCode) + #13#10 +
                   'Por favor, instálelo manualmente.',
                   mbError, MB_OK);
          end;
        except
          Log('Excepción al instalar VC++ x86: ' + GetExceptionMessage);
        end;
      end
      else
      begin
        Log('ERROR: Archivo VC_redist.x86.exe no encontrado en ' + VCppX86Path);
      end;
    end;
    
    // 3. Install VC++ Redistributable x64 if selected
    if WizardIsTaskSelected('vcpp_x64') then
    begin
      if FileExists(VCppX64Path) then
      begin
        Log('Instalando Visual C++ Redistributable x64...');
        try
          if Exec(VCppX64Path, '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
          begin
            Log('Visual C++ Redistributable x64 instalado correctamente (Exit code: ' + IntToStr(ResultCode) + ')');
          end
          else
          begin
            Log('ERROR al instalar Visual C++ Redistributable x64 (Exit code: ' + IntToStr(ResultCode) + ')');
            MsgBox('Error al instalar Visual C++ Redistributable x64 automáticamente.' + #13#10 +
                   'Código de salida: ' + IntToStr(ResultCode) + #13#10 +
                   'Por favor, instálelo manualmente.',
                   mbError, MB_OK);
          end;
        except
          Log('Excepción al instalar VC++ x64: ' + GetExceptionMessage);
        end;
      end
      else
      begin
        Log('ERROR: Archivo VC_redist.x64.exe no encontrado en ' + VCppX64Path);
      end;
    end;
  end;
end;

function NeedRestart(): Boolean;
begin
  // .NET 8 typically doesn't require restart, but VC++ might
  Result := False;
  if WizardIsTaskSelected('vcpp_x86') or WizardIsTaskSelected('vcpp_x64') then
  begin
    Result := True;
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
  
  if FileExists(DotNetInstallerPathX86) then
  begin
    try
      DeleteFile(DotNetInstallerPathX86);
    except
    end;
  end;
end;
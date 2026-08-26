using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Globalization;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AdbWirelessToolkitGUI
{
    public partial class App : Application
    {
        private const string SettingsFileName = "settings.json";
        private const string DefaultLanguage = "es-419";
        private static readonly string[] SupportedLanguages = 
        {
            "es-419", "en-US", "ru", "pt-BR", "ja", "zh-Hans"
        };

        private string _logFilePath = string.Empty;
        private readonly object _logLock = new object();

        protected override void OnStartup(StartupEventArgs e)
        {
            // Initialize logging system FIRST - before anything else
            InitializeLogging();
            LogToFile("Iniciando AdbWirelessToolkitGUI - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            LogToFile($"Versión: {Assembly.GetExecutingAssembly().GetName().Version}");
            LogToFile($"Directorio de ejecución: {AppDomain.CurrentDomain.BaseDirectory}");
            LogToFile($"OS: {Environment.OSVersion}");
            LogToFile($".NET Runtime: {Environment.Version}");
            LogToFile($"CommandLine: {Environment.CommandLine}");

            // CRITICAL: Run dependency verification BEFORE loading UI
            var verificationResult = RunPreStartupVerification();
            if (!verificationResult.IsSuccess)
            {
                // Log the failure details
                LogToFile("=== VERIFICACIÓN FALLIDA ===");
                foreach (var issue in verificationResult.Issues)
                {
                    LogToFile($"FALTA: {issue}");
                }
                LogToFile("============================");

                // Generate diagnostic log file
                string logFilePath = GenerateDiagnosticLog(verificationResult);
                
                // Show user-friendly dialog
                ShowMissingDependenciesDialog(verificationResult, logFilePath);
                
                // Exit gracefully
                Environment.Exit(1);
                return;
            }

            // Subscribe to ALL unhandled exception events
            SetupGlobalExceptionHandling();

            // Load language before UI initializes
            string language = LoadLanguageFromSettings();
            ApplyLanguage(language);

            base.OnStartup(e);
        }

        private (bool IsSuccess, List<string> Issues) RunPreStartupVerification()
        {
            var issues = new List<string>();

            // a) Verify PlatformTools critical files
            string platformToolsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlatformTools");
            var requiredPlatformTools = new[] { "adb.exe", "fastboot.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" };
            
            if (!Directory.Exists(platformToolsDir))
            {
                issues.Add($"Carpeta PlatformTools no encontrada en: {platformToolsDir}");
            }
            else
            {
                foreach (var file in requiredPlatformTools)
                {
                    string filePath = Path.Combine(platformToolsDir, file);
                    if (!File.Exists(filePath))
                    {
                        issues.Add($"Archivo crítico faltante en PlatformTools: {file}");
                    }
                }
            }

            // b) Verify .NET 8.0.30 Desktop Runtime
            if (!IsDotNetDesktopRuntimeInstalled("8.0.30"))
            {
                issues.Add(".NET 8.0.30 Desktop Runtime no está instalado en el sistema");
            }

            // c) Verify VC++ Redistributables (2015-2022)
            if (!IsVCppRedistributableInstalled())
            {
                issues.Add("Visual C++ Redistributable (2015-2022) no detectado");
            }

            // d) Verify critical resources
            var requiredResources = new[] 
            { 
                "Assets/Android-Logo-2008.ico",
                "Assets/Android-Logo-2008.png",
                "Languages/es-419.xaml",
                "Languages/en-US.xaml"
            };

            foreach (var resource in requiredResources)
            {
                string resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resource);
                if (!File.Exists(resourcePath))
                {
                    issues.Add($"Recurso crítico faltante: {resource}");
                }
            }

            return (issues.Count == 0, issues);
        }

        private bool IsDotNetDesktopRuntimeInstalled(string version)
        {
            try
            {
                // Check registry for .NET Desktop Runtime
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App"))
                {
                    if (key != null)
                    {
                        var versionValue = key.GetValue("Version")?.ToString();
                        if (!string.IsNullOrEmpty(versionValue) && versionValue.StartsWith(version))
                        {
                            return true;
                        }
                    }
                }

                // Fallback: check if dotnet --list-runtimes shows the version
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--list-runtimes",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Contains($"Microsoft.WindowsDesktop.App {version}");
            }
            catch
            {
                return false;
            }
        }

        private bool IsVCppRedistributableInstalled()
        {
            try
            {
                // Check for VC++ 2015-2022 Redistributable (x64)
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64"))
                {
                    if (key != null)
                    {
                        var installed = key.GetValue("Installed");
                        if (installed != null && installed.ToString() == "1")
                            return true;
                    }
                }

                // Check x86
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86"))
                {
                    if (key != null)
                    {
                        var installed = key.GetValue("Installed");
                        if (installed != null && installed.ToString() == "1")
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateDiagnosticLog((bool IsSuccess, List<string> Issues) verificationResult)
        {
            string logDir;
            string logFileName = $"AdbWirelessToolkitGUI_{DateTime.Now:ddMMyyyy_HH-mm-ss}.txt";

            try
            {
                // Try to create Logs folder in the installation directory
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string logDirPath = Path.Combine(exeDir, "Logs");
                Directory.CreateDirectory(logDirPath);
                logDir = logDirPath;
            }
            catch
            {
                // Fallback to LocalAppData
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDirPath = Path.Combine(appDataPath, "AdbWirelessToolkitGUI", "Logs");
                Directory.CreateDirectory(logDirPath);
                logDir = logDirPath;
            }

            string logFilePath = Path.Combine(logDir, $"AdbWirelessToolkitGUI_{DateTime.Now:ddMMyyyy_HH-mm-ss}.txt");

            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== AdbWirelessToolkitGUI - Informe de Diagnóstico ===");
                sb.AppendLine($"Fecha/Hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"Versión: {Assembly.GetExecutingAssembly().GetName().Version}");
                sb.AppendLine($"Directorio Base: {AppDomain.CurrentDomain.BaseDirectory}");
                sb.AppendLine($"OS: {Environment.OSVersion}");
                sb.AppendLine($".NET Runtime: {Environment.Version}");
                sb.AppendLine($"Arquitectura: {RuntimeInformation.OSArchitecture}");
                sb.AppendLine($"Proceso: {Process.GetCurrentProcess().Id}");
                sb.AppendLine();
                sb.AppendLine($"=== RESULTADO DE VERIFICACIÓN ===");
                sb.AppendLine($"Estado: {(verificationResult.IsSuccess ? "EXITOSO" : "FALLIDO")}");
                sb.AppendLine($"Total de problemas: {verificationResult.Issues.Count}");
                sb.AppendLine();
                
                if (verificationResult.Issues.Count > 0)
                {
                    sb.AppendLine("=== PROBLEMAS DETECTADOS ===");
                    foreach (var issue in verificationResult.Issues)
                    {
                        sb.AppendLine($"[ERROR] {issue}");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("=== INFORMACIÓN DEL SISTEMA ===");
                sb.AppendLine($"Directorio Base: {AppDomain.CurrentDomain.BaseDirectory}");
                sb.AppendLine($"PlatformTools: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlatformTools")}");
                sb.AppendLine($"Archivos en PlatformTools:");
                string ptDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlatformTools");
                if (Directory.Exists(ptDir))
                {
                    foreach (var f in Directory.GetFiles(ptDir))
                    {
                        sb.AppendLine($"  - {Path.GetFileName(f)}");
                    }
                }
                else
                {
                    sb.AppendLine("  (carpeta no existe)");
                }

                string logContent = sb.ToString();
                File.WriteAllText(logFilePath, logContent);
            }
            catch (Exception ex)
            {
                // Fallback: write to temp
                string fallbackPath = Path.Combine(Path.GetTempPath(), $"AdbWirelessToolkitGUI_{DateTime.Now:ddMMyyyy_HH-mm-ss}.txt");
                try { File.WriteAllText(fallbackPath, $"Error generando log: {ex.Message}"); } catch { }
                return fallbackPath;
            }

            return logFilePath;
        }

        private void ShowMissingDependenciesDialog((bool IsSuccess, List<string> Issues) result, string logFilePath)
        {
            try
            {
                string message = "La aplicación no puede iniciarse porque faltan componentes requeridos:\n\n";
                
                foreach (var issue in result.Issues)
                {
                    message += $"• {issue}\n";
                }

                message += $"\nSe ha generado un informe detallado en:\n{logFilePath}\n\n";
                message += "Por favor, instale los componentes faltantes e intente nuevamente.\n";
                message += "Para .NET 8.0.30 Desktop Runtime, descargue desde:\n";
                message += "https://dotnet.microsoft.com/download/dotnet/8.0\n\n";
                message += "Para Visual C++ Redistributable:\n";
                message += "https://learn.microsoft.com/es-es/cpp/windows/latest-supported-vc-redist";

                MessageBox.Show(message, "Dependencias Faltantes - AdbWirelessToolkitGUI", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // Ignore UI errors
            }
        }

        private void InitializeLogging()
        {
            try
            {
                // Try to write in the same directory as the executable first
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                _logFilePath = Path.Combine(exeDir, "AdbWirelessToolkitGUI_Log.txt");
                
                // Test write access
                using (var testStream = File.Open(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    // Just test if we can write
                }
            }
            catch (Exception)
            {
                // Fallback to LocalAppData if no write permission in exe directory
                try
                {
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string appFolder = Path.Combine(appDataPath, "AdbWirelessToolkitGUI");
                    
                    if (!Directory.Exists(appFolder))
                    {
                        Directory.CreateDirectory(appFolder);
                    }
                    
                    _logFilePath = Path.Combine(appFolder, "AdbWirelessToolkitGUI_Log.txt");
                }
                catch
                {
                    // Last resort - use temp path
                    _logFilePath = Path.Combine(Path.GetTempPath(), "AdbWirelessToolkitGUI_Log.txt");
                }
            }

            // Write initial log entry
            try
            {
                string initMsg = $"Iniciando AdbWirelessToolkitGUI - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n";
                File.AppendAllText(_logFilePath, initMsg);
            }
            catch
            {
                // If we can't write log anywhere, we continue anyway
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                lock (_logLock)
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n";
                    File.AppendAllText(_logFilePath, logEntry);
                }
            }
            catch
            {
                // Ignore logging failures
            }
        }

        private void SetupGlobalExceptionHandling()
        {
            // 1. Handle exceptions on the UI thread (WPF Dispatcher)
            DispatcherUnhandledException += (sender, e) =>
            {
                LogException(e.Exception, "DispatcherUnhandledException (UI Thread)");
                e.Handled = true; // Prevent crash
                ShowFatalErrorAndExit(e.Exception);
            };

            // 2. Handle exceptions on non-UI threads
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    LogException(ex, "AppDomain.UnhandledException (Non-UI Thread)");
                }
                else
                {
                    LogToFile($"[FATAL] UnhandledException: {e.ExceptionObject}");
                }
                
                // Force flush and exit
                Environment.Exit(1);
            };

            // 3. Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved(); // Prevent crash
            };

            // 4. Handle process exit to flush logs
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                LogToFile("=== Proceso finalizado ===");
            };
        }

        private void LogException(Exception ex, string source)
        {
            if (ex == null) return;

            try
            {
                lock (_logLock)
                {
                    string entry = $"\n=== EXCEPCIÓN NO CONTROLADA [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ===\n";
                    entry += $"Fuente: {source}\n";
                    entry += $"Tipo: {ex.GetType().FullName}\n";
                    entry += $"Mensaje: {ex.Message}\n";
                    entry += $"StackTrace:\n{ex.StackTrace}\n";
                    
                    if (ex.InnerException != null)
                    {
                        entry += "\n--- Inner Exception ---\n";
                        entry += $"Tipo: {ex.InnerException.GetType().FullName}\n";
                        entry += $"Mensaje: {ex.InnerException.Message}\n";
                        entry += $"StackTrace:\n{ex.InnerException.StackTrace}\n";
                    }
                    
                    entry += "=== FIN EXCEPCIÓN ===\n\n";
                    
                    File.AppendAllText(_logFilePath, entry);
                }
            }
            catch
            {
                // Ignore logging failures
            }
        }

        private void ShowFatalErrorAndExit(Exception ex)
        {
            try
            {
                string msg = $"La aplicación ha encontrado un error crítico y debe cerrarse.\n\n" +
                           $"Error: {ex.Message}\n\n" +
                           $"Se ha guardado el detalle del error en:\n{_logFilePath}\n\n" +
                           "Por favor, reporte este error adjuntando el archivo de log.";
                
                MessageBox.Show(msg, "Error Crítico - AdbWirelessToolkitGUI", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // Ignore UI errors
            }
            
            // Force exit
            Environment.Exit(1);
        }

        private string LoadLanguageFromSettings()
        {
            try
            {
                string settingsPath = GetSettingsFilePath();
                
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                        if (settings != null && !string.IsNullOrEmpty(settings.Language))
                        {
                            string lang = settings.Language.Trim();
                            if (Array.Exists(SupportedLanguages, l => l.Equals(lang, StringComparison.OrdinalIgnoreCase)))
                            {
                                return lang;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error loading settings: {ex.Message}");
            }

            // CRITICAL: Always fallback to Spanish (es-419) if anything fails
            return DefaultLanguage;
        }

        private void ApplyLanguage(string languageCode)
        {
            try
            {
                // Remove any existing language dictionary (keep the first one which is es-419 fallback)
                var mergedDicts = Resources.MergedDictionaries;
                
                // Remove all but the first dictionary (es-419 fallback)
                while (mergedDicts.Count > 1)
                {
                    mergedDicts.RemoveAt(mergedDicts.Count - 1);
                }

                // Load the selected language
                string dictPath = $"Languages/{languageCode}.xaml";
                var dict = new ResourceDictionary
                {
                    Source = new Uri(dictPath, UriKind.Relative)
                };
                mergedDicts.Add(dict);

                // Set culture for WPF
                var culture = new System.Globalization.CultureInfo(languageCode);
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error applying language {languageCode}: {ex.Message}");
                // If language fails, es-419 is already loaded as fallback
            }
        }

        public static void ChangeLanguage(string languageCode)
        {
            if (Array.Exists(SupportedLanguages, l => l.Equals(languageCode, StringComparison.OrdinalIgnoreCase)))
            {
                if (Current is App app)
                {
                    app.ApplyLanguage(languageCode);
                    SaveLanguageToSettings(languageCode);
                }
            }
        }

        private static void SaveLanguageToSettings(string languageCode)
        {
            try
            {
                string settingsPath = GetSettingsFilePath();
                var settings = new AppSettings { Language = languageCode };
                string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error saving settings: {ex.Message}");
            }
        }

        private static string GetSettingsFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "AdbWirelessToolkitGUI");
            
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            return Path.Combine(appFolder, "settings.json");
        }

        internal class AppSettings
        {
            public string Language { get; set; } = "es-419";
        }
    }
}
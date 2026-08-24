using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Markup;

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

        protected override void OnStartup(StartupEventArgs e)
        {
            // Load language before UI initializes
            string language = LoadLanguageFromSettings();
            ApplyLanguage(language);

            base.OnStartup(e);
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
                        var settings = JsonSerializer.Deserialize<AppSettings>(json);
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
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
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
            
            return Path.Combine(appFolder, SettingsFileName);
        }
    }

    internal class AppSettings
    {
        public string Language { get; set; } = "es-419";
    }
}
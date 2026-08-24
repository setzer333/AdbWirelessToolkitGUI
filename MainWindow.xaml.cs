using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace AdbWirelessToolkitGUI;

public partial class MainWindow : Window
{
    private readonly CancellationTokenSource _globalCts = new();
    private bool _isIndeterminateProgress = false;

    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) => _globalCts.Cancel();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await ProfileManager.InitializeAsync();
    }

    private void Log(string message)
    {
        Dispatcher.BeginInvoke(() =>
        {
            LogConsole.AppendText($"{DateTime.Now:HH:mm:ss} | {message}\n");
            LogConsole.ScrollToEnd();
        });
    }

    private void UpdateProgress(TransferProgress progress)
    {
        Dispatcher.BeginInvoke(() =>
        {
            switch (progress.Type)
            {
                case TransferType.Install:
                    if (progress.Percent < 0)
                    {
                        // Indeterminate for install
                        if (!_isIndeterminateProgress)
                        {
                            TransferProgressBar.IsIndeterminate = true;
                            _isIndeterminateProgress = true;
                        }
                        ProgressTextBlock.Text = TryFindResource("InstallingText") as string ?? "Instalando...";
                    }
                    else
                    {
                        // Final success
                        TransferProgressBar.IsIndeterminate = false;
                        _isIndeterminateProgress = false;
                        TransferProgressBar.Value = progress.Percent;
                        ProgressTextBlock.Text = $"{progress.Percent}%";
                    }
                    break;

                case TransferType.Sideload:
                    TransferProgressBar.IsIndeterminate = false;
                    _isIndeterminateProgress = false;
                    if (progress.Percent >= 0)
                    {
                        TransferProgressBar.Value = progress.Percent;
                        ProgressTextBlock.Text = $"{progress.Percent}%";
                    }
                    break;

                default: // Standard
                    TransferProgressBar.IsIndeterminate = false;
                    _isIndeterminateProgress = false;
                    if (progress.Percent >= 0)
                    {
                        TransferProgressBar.Value = progress.Percent;
                        ProgressTextBlock.Text = $"{progress.Percent}%";
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(progress.Speed))
            {
                SpeedTextBlock.Text = progress.Speed;
            }
        });
    }

    private void ResetProgressUI()
    {
        Dispatcher.BeginInvoke(() =>
        {
            TransferProgressBar.IsIndeterminate = false;
            _isIndeterminateProgress = false;
            TransferProgressBar.Value = 0;
            ProgressTextBlock.Text = TryFindResource("ProgressLabel") as string ?? "0%";
            SpeedTextBlock.Text = TryFindResource("SpeedLabel") as string ?? "0 B/s";
        });
    }

    private void SetButtonsEnabled(bool enabled)
    {
        Dispatcher.BeginInvoke(() =>
        {
            foreach (var child in FindVisualChildren<Button>(this))
            {
                child.IsEnabled = enabled;
            }
        });
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null) yield break;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            if (child is T t) yield return t;
            foreach (var childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
        }
    }

    // ==================== CONEXIÓN INALÁMBRICA ====================

    private async void ScanNetwork_Click(object sender, RoutedEventArgs e)
    {
        ResetProgressUI();
        Log(TryFindResource("ScanningNetwork") as string ?? "=== Escaneando Red Local (Ping .1-.99) ===");
        SetButtonsEnabled(false);
        try
        {
            await AdbEngine.ScanLocalNetworkAsync(
                line => Log(line),
                _globalCts.Token);
            Log(TryFindResource("ScanCompleted") as string ?? "=== Escaneo completado ===");
        }
        catch (OperationCanceledException)
        {
            Log(TryFindResource("OperationCancelled") as string ?? "=== Operación cancelada ===");
        }
        catch (Exception ex)
        {
            Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async void ScanMdns_Click(object sender, RoutedEventArgs e)
    {
        ResetProgressUI();
        Log(TryFindResource("ScanningMdns") as string ?? "=== Escaneo mDNS Nativo (adb mdns services) ===");
        SetButtonsEnabled(false);
        try
        {
            await AdbEngine.ScanMdnsAsync(
                line => Log(line),
                _globalCts.Token);
            Log(TryFindResource("MdnsScanCompleted") as string ?? "=== Escaneo mDNS finalizado ===");
        }
        catch (OperationCanceledException)
        {
            Log(TryFindResource("OperationCancelled") as string ?? "=== Escaneo mDNS cancelado ===");
        }
        catch (Exception ex)
        {
            Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async void PairDevice_Click(object sender, RoutedEventArgs e)
    {
        string hostPort = txtHostPort.Text?.Trim() ?? string.Empty;
        string pairCode = txtPairCode.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(hostPort))
        {
            Log(TryFindResource("ErrorHostPortRequired") as string ?? "ERROR: Debe ingresar HOST:PORT (ej: 192.168.1.100:5555)");
            txtHostPort.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(pairCode))
        {
            Log(TryFindResource("ErrorPairCodeRequired") as string ?? "ERROR: Debe ingresar el CÓDIGO DE EMPAREJAMIENTO (6 dígitos)");
            txtPairCode.Focus();
            return;
        }

        if (!pairCode.All(char.IsDigit) || pairCode.Length != 6)
        {
            Log(TryFindResource("ErrorPairCodeFormat") as string ?? "ERROR: El código de emparejamiento debe ser de 6 dígitos numéricos");
            txtPairCode.Focus();
            return;
        }

        ResetProgressUI();
        Log(string.Format(TryFindResource("PairingDevice") as string ?? "=== Emparejando dispositivo: {0} ===", hostPort));
        SetButtonsEnabled(false);
        try
        {
            var result = await AdbEngine.ExecuteCommandAsync(
                $"pair {hostPort} {pairCode}",
                line => Log(line),
                cancellationToken: _globalCts.Token);
            
            Log(TryFindResource("PairingCompleted") as string ?? "=== Emparejamiento completado ===");

            // Check for success and prompt to save profile
            if (result.Contains("Successfully", StringComparison.OrdinalIgnoreCase) || 
                result.Contains("Success", StringComparison.OrdinalIgnoreCase))
            {
                await PromptSaveProfileAsync(hostPort, pairCode);
            }
        }
        catch (OperationCanceledException)
        {
            Log(TryFindResource("OperationCancelled") as string ?? "=== Emparejamiento cancelado ===");
        }
        catch (Exception ex)
        {
            Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async Task PromptSaveProfileAsync(string hostPort, string pairCode)
    {
        var dialog = new SaveProfileWindow(hostPort, pairCode, ProfileManager.CurrentCount >= ProfileManager.MaxProfileCount);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            if (dialog.OverwriteProfile != null)
            {
                // User chose to overwrite existing
                await ProfileManager.RemoveProfileAsync(dialog.OverwriteProfile);
            }
            await ProfileManager.AddOrUpdateProfileAsync(hostPort, pairCode, dialog.ProfileName);
            Log(string.Format(TryFindResource("ProfileSaved") as string ?? "[PERFIL] Guardado: {0}", dialog.ProfileName ?? hostPort));
        }
    }

    private void ProfileMenu_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileManager.Profiles.Count == 0)
        {
            Log(TryFindResource("NoProfiles") as string ?? "[PERFIL] No hay perfiles guardados. Empareja un dispositivo y guarda el perfil.");
            return;
        }

        var contextMenu = new ContextMenu();

        foreach (var profile in ProfileManager.Profiles)
        {
            var item = new MenuItem
            {
                Header = profile.DisplayName,
                Tag = profile,
                Style = (Style)FindResource("MenuItemStyle")
            };
            item.Click += ProfileMenuItem_Click;
            contextMenu.Items.Add(item);
        }

        // Separator and clear option
        contextMenu.Items.Add(new Separator());
        var clearItem = new MenuItem
        {
            Header = TryFindResource("ClearAllProfiles") as string ?? "🗑 Limpiar todos los perfiles",
            Style = (Style)FindResource("MenuItemStyle")
        };
        clearItem.Click += async (s, ev) => 
        {
            var result = MessageBox.Show(
                TryFindResource("ConfirmClearProfiles") as string ?? "¿Eliminar todos los perfiles guardados?",
                TryFindResource("ConfirmTitle") as string ?? "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await ProfileManager.ClearAllProfilesAsync();
                Log(TryFindResource("AllProfilesCleared") as string ?? "[PERFIL] Todos los perfiles eliminados");
            }
        };
        contextMenu.Items.Add(clearItem);

        contextMenu.PlacementTarget = btnProfileMenu;
        contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        contextMenu.IsOpen = true;
    }

    private void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.Tag is DeviceProfile profile)
        {
            txtHostPort.Text = profile.HostPort;
            txtPairCode.Text = profile.PairingCode;
            Log(string.Format(TryFindResource("ProfileLoaded") as string ?? "[PERFIL] Cargado: {0}", profile.DisplayName));
            
            // Update LastUsed
            _ = Task.Run(async () =>
            {
                profile.LastUsed = DateTime.Now;
                await ProfileManager.SaveProfilesAsync();
            });
        }
    }

    private async void Devices_Click(object sender, RoutedEventArgs e)
    {
        ResetProgressUI();
        Log(TryFindResource("GettingDevices") as string ?? "=== Obteniendo lista de dispositivos ===");
        SetButtonsEnabled(false);
        try
        {
            var result = await AdbEngine.ExecuteCommandAsync(
                "devices",
                line => Log(line),
                cancellationToken: _globalCts.Token);
            Log(TryFindResource("DevicesObtained") as string ?? "=== Lista de dispositivos obtenida ===");
        }
        catch (OperationCanceledException)
        {
            Log(TryFindResource("OperationCancelled") as string ?? "=== Operación cancelada ===");
        }
        catch (Exception ex)
        {
            Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async void RestartServer_Click(object sender, RoutedEventArgs e)
    {
        ResetProgressUI();
        Log(TryFindResource("RestartingServer") as string ?? "=== Reiniciando servidor ADB ===");
        SetButtonsEnabled(false);
        try
        {
            Log(TryFindResource("StoppingServer") as string ?? "--- Paso 1: Deteniendo servidor (adb kill-server) ---");
            await AdbEngine.ExecuteCommandAsync(
                "kill-server",
                line => Log(line),
                TimeSpan.FromSeconds(10),
                _globalCts.Token);

            Log(TryFindResource("Waiting") as string ?? "--- Esperando 1 segundo ---");
            await Task.Delay(1000, _globalCts.Token);

            Log(TryFindResource("StartingServer") as string ?? "--- Paso 2: Iniciando servidor (adb start-server) ---");
            await AdbEngine.ExecuteCommandAsync(
                "start-server",
                line => Log(line),
                TimeSpan.FromSeconds(10),
                _globalCts.Token);

            Log(TryFindResource("ServerRestarted") as string ?? "=== Servidor ADB reiniciado correctamente ===");
        }
        catch (OperationCanceledException)
        {
            Log(TryFindResource("OperationCancelled") as string ?? "=== Reinicio de servidor cancelado ===");
        }
        catch (Exception ex)
        {
            Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    // ==================== GESTIÓN DE DISPOSITIVOS ====================

    private async void InstallApk_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Seleccionar APK",
            Filter = "Archivos APK (*.apk)|*.apk",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ResetProgressUI();
            Log(string.Format(TryFindResource("InstallingApk") as string ?? "=== Instalando APK: {0} ===", openFileDialog.FileName));
            SetButtonsEnabled(false);
            try
            {
                var result = await AdbEngine.ExecuteTransferAsync(
                    $"install -r \"{openFileDialog.FileName}\"",
                    line => Log(line),
                    progress => UpdateProgress(progress),
                    TimeSpan.FromMinutes(10),
                    _globalCts.Token);
                Log(TryFindResource("InstallCompleted") as string ?? "=== Instalación completada ===");
            }
            catch (OperationCanceledException)
            {
                Log(TryFindResource("OperationCancelled") as string ?? "=== Instalación cancelada ===");
            }
            catch (Exception ex)
            {
                Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }
    }

    private async void SideloadApk_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Seleccionar APK para Sideload",
            Filter = "Archivos APK (*.apk)|*.apk",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ResetProgressUI();
            Log(string.Format(TryFindResource("SideloadStarting") as string ?? "=== Iniciando Sideload APK: {0} ===", openFileDialog.FileName));
            Log(TryFindResource("SideloadWarning") as string ?? "⚠ Asegúrate de que el dispositivo esté en modo 'Recovery' → 'Apply update from ADB'");
            SetButtonsEnabled(false);
            try
            {
                var result = await AdbEngine.ExecuteTransferAsync(
                    $"sideload \"{openFileDialog.FileName}\"",
                    line => Log(line),
                    progress => UpdateProgress(progress),
                    TimeSpan.FromMinutes(15),
                    _globalCts.Token);
                Log(TryFindResource("SideloadCompleted") as string ?? "=== Sideload completado ===");
            }
            catch (OperationCanceledException)
            {
                Log(TryFindResource("OperationCancelled") as string ?? "=== Sideload cancelado ===");
            }
            catch (Exception ex)
            {
                Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }
    }

    private async void TransferFile_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Seleccionar archivo a transferir",
            Filter = "Todos los archivos (*.*)|*.*",
            CheckFileExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var inputDialog = new InputDialog("Transferir archivo", "Ruta destino en dispositivo (ej: /sdcard/Download/):");
            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.ResponseText))
            {
                ResetProgressUI();
                Log(string.Format(TryFindResource("TransferStarting") as string ?? "=== Transfiriendo: {0} -> {1} ===", openFileDialog.FileName, inputDialog.ResponseText));
                SetButtonsEnabled(false);
                try
                {
                    var result = await AdbEngine.ExecuteTransferAsync(
                        $"push \"{openFileDialog.FileName}\" \"{inputDialog.ResponseText}\"",
                        line => Log(line),
                        progress => UpdateProgress(progress),
                        TimeSpan.FromMinutes(30),
                        _globalCts.Token);
                    Log(TryFindResource("TransferCompleted") as string ?? "=== Transferencia completada ===");
                }
                catch (OperationCanceledException)
                {
                    Log(TryFindResource("OperationCancelled") as string ?? "=== Transferencia cancelada ===");
                }
                catch (Exception ex)
                {
                    Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
                }
                finally
                {
                    SetButtonsEnabled(true);
                }
            }
        }
    }

    private async void Reboot_Click(object sender, RoutedEventArgs e)
    {
        ResetProgressUI();
        Log(TryFindResource("Rebooting") as string ?? "=== Reiniciando dispositivo (Sistema) ===");
        SetButtonsEnabled(false);
        try
        {
            var result = await AdbEngine.ExecuteCommandAsync(
                "reboot",
                line => Log(line),
                cancellationToken: _globalCts.Token);
            Log(TryFindResource("RebootInitiated") as string ?? "=== Reinicio iniciado ===");
        }
        catch (OperationCanceledException)
        {
            Log(TryFindResource("OperationCancelled") as string ?? "=== Reinicio cancelado ===");
        }
        catch (Exception ex)
        {
            Log($"{(TryFindResource("ErrorPrefix") as string ?? "ERROR: ")}{ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    // ==================== TERMINAL MANUAL ====================

    private void txtManualCommand_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RunManualCommand_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private async void RunManualCommand_Click(object sender, RoutedEventArgs e)
    {
        var rawCommand = txtManualCommand.Text?.Trim();
        if (string.IsNullOrWhiteSpace(rawCommand))
        {
            Log(TryFindResource("ErrorEmptyCommand") as string ?? "[MANUAL] Comando vacío. Escribe un comando ADB.");
            return;
        }

        string arguments = rawCommand.StartsWith("adb ", StringComparison.OrdinalIgnoreCase)
            ? rawCommand[4..].TrimStart()
            : rawCommand;

        ResetProgressUI();
        Log(string.Format(TryFindResource("ManualCommand") as string ?? "[MANUAL] adb {0}", arguments));
        SetButtonsEnabled(false);
        txtManualCommand.Clear();

        try
        {
            var result = await AdbEngine.ExecuteCommandAsync(
                arguments,
                line => Log(line),
                cancellationToken: _globalCts.Token);
            Log(TryFindResource("ManualCompleted") as string ?? "[MANUAL] Comando finalizado.");
        }
        catch (OperationCanceledException)
        {
            Log(TryFindResource("OperationCancelled") as string ?? "[MANUAL] Comando cancelado.");
        }
        catch (Exception ex)
        {
            Log($"{(TryFindResource("ErrorPrefix") as string ?? "[MANUAL] ERROR: ")}{ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    // ==================== HERRAMIENTAS DE RESPALDO ====================

    private void OpenCmd_Click(object sender, RoutedEventArgs e)
    {
        string platformToolsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlatformTools");
        
        if (!Directory.Exists(platformToolsPath))
        {
            Log(string.Format(TryFindResource("CmdNotFound") as string ?? "[CMD] ERROR: No se encuentra PlatformTools en: {0}", platformToolsPath));
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = platformToolsPath,
                UseShellExecute = true
            };
            Process.Start(psi);
            Log(string.Format(TryFindResource("CmdOpened") as string ?? "[CMD] Consola externa abierta en: {0}", platformToolsPath));
        }
        catch (Exception ex)
        {
            Log(string.Format(TryFindResource("CmdError") as string ?? "[CMD] ERROR al abrir consola: {0}", ex.Message));
        }
    }
}

// ==================== DIÁLOGOS AUXILIARES ====================

public class InputDialog : Window
{
    public string ResponseText { get; private set; } = "";
    private readonly TextBox _inputTextBox;

    public InputDialog(string title, string label)
    {
        Title = title;
        Width = 400;
        Height = 180;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = System.Windows.Media.Brushes.White;

        var stack = new StackPanel { Margin = new Thickness(20) };
        stack.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 5) });
        _inputTextBox = new TextBox { Height = 30 };
        stack.Children.Add(_inputTextBox);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
        var okBtn = new Button { Content = "Aceptar", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        var cancelBtn = new Button { Content = "Cancelar", Width = 80 };
        
        okBtn.Click += (s, e) => { ResponseText = _inputTextBox.Text; DialogResult = true; };
        cancelBtn.Click += (s, e) => DialogResult = false;
        
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        stack.Children.Add(btnPanel);

        Content = stack;
        _inputTextBox.Focus();
    }
}

public class SaveProfileWindow : Window
{
    public string? ProfileName { get; private set; }
    public DeviceProfile? OverwriteProfile { get; private set; }

    private readonly string _hostPort;
    private readonly string _pairCode;
    private readonly bool _isFull;
    private readonly ComboBox? _profileCombo;

    public SaveProfileWindow(string hostPort, string pairCode, bool isFull)
    {
        _hostPort = hostPort;
        _pairCode = pairCode;
        _isFull = isFull;

        Title = TryFindResource("SaveProfileTitle") as string ?? "Guardar Perfil de Emparejamiento";
        Width = 450;
        Height = _isFull ? 280 : 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = System.Windows.Media.Brushes.White;

        var stack = new StackPanel { Margin = new Thickness(20) };

        stack.Children.Add(new TextBlock 
        { 
            Text = TryFindResource("SaveProfileMessage") as string ?? "El emparejamiento fue exitoso. ¿Deseas guardar este perfil para uso futuro?", 
            TextWrapping = TextWrapping.Wrap, 
            Margin = new Thickness(0, 0, 0, 10) 
        });

        stack.Children.Add(new TextBlock { Text = string.Format(TryFindResource("SaveProfileDevice") as string ?? "Dispositivo: {0}", hostPort), FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 5) });

        if (_isFull)
        {
            stack.Children.Add(new TextBlock 
            { 
                Text = TryFindResource("SaveProfileLimitWarning") as string ?? "⚠ Límite de 10 perfiles alcanzado. Selecciona uno para sobrescribir:", 
                Foreground = System.Windows.Media.Brushes.OrangeRed,
                TextWrapping = TextWrapping.Wrap, 
                Margin = new Thickness(0, 0, 0, 5) 
            });

            _profileCombo = new ComboBox 
            { 
                Height = 30, 
                Margin = new Thickness(0, 0, 0, 10),
                ItemsSource = ProfileManager.Profiles,
                DisplayMemberPath = "DisplayName"
            };
            _profileCombo.SelectionChanged += (s, e) => 
            {
                OverwriteProfile = _profileCombo.SelectedItem as DeviceProfile;
            };
            if (ProfileManager.Profiles.Count > 0)
                _profileCombo.SelectedIndex = 0;
            stack.Children.Add(_profileCombo);
        }
        else
        {
            stack.Children.Add(new TextBlock { Text = TryFindResource("SaveProfileNameLabel") as string ?? "Nombre del perfil (opcional):", Margin = new Thickness(0, 0, 0, 2) });
            var nameBox = new TextBox 
            { 
                Height = 30, 
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = TryFindResource("SaveProfileNameTooltip") as string ?? "Deja vacío para usar solo HOST:PORT como nombre"
            };
            nameBox.TextChanged += (s, e) => ProfileName = nameBox.Text.Trim();
            stack.Children.Add(nameBox);
        }

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
        var okBtn = new Button { Content = _isFull ? (TryFindResource("SaveProfileOverwriteBtn") as string ?? "Sobrescribir") : (TryFindResource("SaveProfileSaveBtn") as string ?? "Guardar"), Width = 100, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
        var cancelBtn = new Button { Content = TryFindResource("SaveProfileCancelBtn") as string ?? "No guardar", Width = 100, IsCancel = true };
        
        okBtn.Click += (s, e) => DialogResult = true;
        cancelBtn.Click += (s, e) => DialogResult = false;
        
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        stack.Children.Add(btnPanel);

        Content = stack;
    }
}
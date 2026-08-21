using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AdbWirelessToolkitGUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogConsole.AppendText($"{DateTime.Now:HH:mm:ss} | {message}\n");
            LogConsole.ScrollToEnd();
        });
    }

    private void UpdateProgress(int percent)
    {
        Dispatcher.Invoke(() =>
        {
            TransferProgressBar.Value = percent;
            ProgressTextBlock.Text = $"{percent}%";
        });
    }

    private void UpdateSpeed(string speed)
    {
        Dispatcher.Invoke(() =>
        {
            SpeedTextBlock.Text = speed;
        });
    }

    // Escaneo mDNS
    private async void MdnsScan_Click(object sender, RoutedEventArgs e)
    {
        Log("=== Escaneo mDNS iniciado ===");
        try
        {
            var result = await AdbEngine.ExecuteCommandAsync(
                "mdns",
                line => Log(line));
            Log($"=== Escaneo completado: {result.Trim()} ===");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
        }
    }

    // Emparejar dispositivo
    private async void PairDevice_Click(object sender, RoutedEventArgs e)
    {
        Log("=== Emparejando dispositivo ===");
        Log("Ingrese la IP y puerto del dispositivo (ej: 192.168.1.100:5555)");
        
        var inputDialog = new InputDialog("Emparejar dispositivo", "IP:Puerto:");
        if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.ResponseText))
        {
            try
            {
                var result = await AdbEngine.ExecuteCommandAsync(
                    $"pair {inputDialog.ResponseText}",
                    line => Log(line));
                Log($"=== Emparejamiento completado ===");
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
            }
        }
    }

    // Conectar por IP
    private async void ConnectIp_Click(object sender, RoutedEventArgs e)
    {
        Log("=== Conectando por IP ===");
        Log("Ingrese la IP del dispositivo (ej: 192.168.1.100:5555)");

        var inputDialog = new InputDialog("Conectar por IP", "IP:Puerto:");
        if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.ResponseText))
        {
            try
            {
                var result = await AdbEngine.ExecuteCommandAsync(
                    $"connect {inputDialog.ResponseText}",
                    line => Log(line));
                Log($"=== Conexión completada ===");
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
            }
        }
    }

    // Instalar APK
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
            Log($"=== Instalando APK: {openFileDialog.FileName} ===");
            try
            {
                var result = await AdbEngine.ExecuteCommandAsync(
                    $"install -r \"{openFileDialog.FileName}\"",
                    line => Log(line));
                Log($"=== Instalación completada ===");
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
            }
        }
    }

    // Transferir archivo
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
                Log($"=== Transfiriendo: {openFileDialog.FileName} -> {inputDialog.ResponseText} ===");
                try
                {
                    var result = await AdbEngine.ExecuteTransferAsync(
                        $"push \"{openFileDialog.FileName}\" \"{inputDialog.ResponseText}\"",
                        line => Log(line),
                        percent => UpdateProgress(percent));
                    Log($"=== Transferencia completada ===");
                }
                catch (Exception ex)
                {
                    Log($"ERROR: {ex.Message}");
                }
            }
        }
    }

    // Reiniciar (Bootloader/Recovery)
    private async void RestartBootloader_Click(object sender, RoutedEventArgs e)
    {
        Log("=== Reiniciando dispositivo ===");
        
        var optionsDialog = new RestartOptionsDialog();
        if (optionsDialog.ShowDialog() == true)
        {
            string mode = optionsDialog.SelectedMode;
            try
            {
                var result = await AdbEngine.ExecuteCommandAsync(
                    $"reboot {mode}",
                    line => Log(line));
                Log($"=== Reinicio a {mode} iniciado ===");
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
            }
        }
    }
}

// Dialog simple para input de texto
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

// Dialog para opciones de reinicio
public class RestartOptionsDialog : Window
{
    public string SelectedMode { get; private set; } = "bootloader";

    public RestartOptionsDialog()
    {
        Title = "Seleccionar modo de reinicio";
        Width = 350;
        Height = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = System.Windows.Media.Brushes.White;

        var stack = new StackPanel { Margin = new Thickness(20) };
        
        stack.Children.Add(new TextBlock { Text = "Seleccione el modo:", FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 10) });

        var rbBootloader = new RadioButton { Content = "Bootloader", IsChecked = true, Margin = new Thickness(0, 5, 0, 0) };
        var rbRecovery = new RadioButton { Content = "Recovery", Margin = new Thickness(0, 5, 0, 0) };
        var rbSystem = new RadioButton { Content = "Sistema (normal)", Margin = new Thickness(0, 5, 0, 0) };
        
        stack.Children.Add(rbBootloader);
        stack.Children.Add(rbRecovery);
        stack.Children.Add(rbSystem);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        var okBtn = new Button { Content = "Aceptar", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
        var cancelBtn = new Button { Content = "Cancelar", Width = 80 };
        
        okBtn.Click += (s, e) => 
        {
            if (rbBootloader.IsChecked == true) SelectedMode = "bootloader";
            else if (rbRecovery.IsChecked == true) SelectedMode = "recovery";
            else SelectedMode = "";
            DialogResult = true; 
        };
        cancelBtn.Click += (s, e) => DialogResult = false;
        
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        stack.Children.Add(btnPanel);

        Content = stack;
    }
}
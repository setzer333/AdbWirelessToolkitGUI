using System.Windows;

namespace AdbWirelessToolkitGUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Eventos de los botones ADB
    private void MdnsScan_Click(object sender, RoutedEventArgs e) => Log("Escaneo mDNS iniciado");

    private void PairDevice_Click(object sender, RoutedEventArgs e) => Log("Emparejando dispositivo");

    private void ConnectIp_Click(object sender, RoutedEventArgs e) => Log("Conectando por IP");

    private void InstallApk_Click(object sender, RoutedEventArgs e) => Log("Instalando APK");

    private void TransferFile_Click(object sender, RoutedEventArgs e) => Log("Iniciando transferencia de archivo");

    private void RestartBootloader_Click(object sender, RoutedEventArgs e) => Log("Reiniciando bootloader");

    // Método para agregar logs a la consola
    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogConsole.AppendText($"{DateTime.Now:HH:mm:ss} | {message}\n");
            // Auto-scroll al final
            LogConsole.ScrollToEnd();
        });
    }
}
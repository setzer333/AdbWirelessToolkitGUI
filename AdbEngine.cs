using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AdbWirelessToolkitGUI;

public enum TransferType
{
    Standard,     // push/pull with percentage
    Install,      // adb install - indeterminate until "Success"
    Sideload      // adb sideload - percentage + speed
}

public record TransferProgress(int Percent, string? Speed = null, TransferType Type = TransferType.Standard);

public static class AdbEngine
{
    private static readonly string AdbPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "PlatformTools",
        "adb.exe");

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    public static async Task<string> ExecuteCommandAsync(
        string arguments,
        Action<string> onOutputReceived,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(AdbPath))
        {
            throw new FileNotFoundException(
                $"ADB no encontrado en: {AdbPath}. Verifica que la carpeta PlatformTools se copió al directorio de salida.");
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = AdbPath,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(AdbPath) ?? AppDomain.CurrentDomain.BaseDirectory
        };

        using var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };
        var outputBuilder = new System.Text.StringBuilder();
        var tcs = new TaskCompletionSource<string>();

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
        });

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                onOutputReceived?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                onOutputReceived?.Invoke($"[ERROR] {e.Data}");
            }
        };

        process.Exited += (sender, e) =>
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(outputBuilder.ToString());
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"No se pudo iniciar ADB. Argumentos: {arguments}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout ?? DefaultTimeout, cancellationToken));
        if (completedTask == tcs.Task)
        {
            return await tcs.Task;
        }

        // Timeout
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch { }

        throw new TimeoutException($"ADB excedió el tiempo límite ({timeout ?? DefaultTimeout}) ejecutando: {arguments}");
    }

    public static async Task<string> ExecuteTransferAsync(
        string arguments,
        Action<string> onOutputReceived,
        Action<TransferProgress> onProgress,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(AdbPath))
        {
            throw new FileNotFoundException(
                $"ADB no encontrado en: {AdbPath}. Verifica que la carpeta PlatformTools se copió al directorio de salida.");
        }

        // Determine transfer type from arguments
        var transferType = TransferType.Standard;
        if (arguments.StartsWith("install", StringComparison.OrdinalIgnoreCase))
            transferType = TransferType.Install;
        else if (arguments.StartsWith("sideload", StringComparison.OrdinalIgnoreCase))
            transferType = TransferType.Sideload;

        var processStartInfo = new ProcessStartInfo
        {
            FileName = AdbPath,
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(AdbPath) ?? AppDomain.CurrentDomain.BaseDirectory
        };

        using var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true };
        var outputBuilder = new System.Text.StringBuilder();
        var tcs = new TaskCompletionSource<string>();

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch { }
        });

        // Enhanced regex patterns for modern ADB output:
        // - Percentage: "45%", "  45%", "45 %"
        // - Speed: "25.5 MB/s", "1.2 GB/s", "512 KB/s"
        // - Combined: "file.apk: 25.5 MB/s (45%)" or "Total xfer: 1.00x (45%)"
        var progressRegex = new Regex(
            @"(?<!\d)(\d{1,3})\s*%(?!\d)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        var speedRegex = new Regex(
            @"([\d.]+)\s*(KB|MB|GB)/s",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        bool installStarted = false;

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data == null) return;

            outputBuilder.AppendLine(e.Data);
            onOutputReceived?.Invoke(e.Data);

            try
            {
                // Parse percentage
                var percentMatch = progressRegex.Match(e.Data);
                int? percent = null;
                if (percentMatch.Success && int.TryParse(percentMatch.Groups[1].Value, out int p))
                {
                    if (p >= 0 && p <= 100)
                        percent = p;
                }

                // Parse speed (MB/s, KB/s, GB/s)
                string? speed = null;
                var speedMatch = speedRegex.Match(e.Data);
                if (speedMatch.Success)
                {
                    speed = $"{speedMatch.Groups[1].Value} {speedMatch.Groups[2].Value}/s";
                }

                // Handle install-specific logic
                if (transferType == TransferType.Install)
                {
                    if (!installStarted && e.Data.Contains("Performing Streamed Install", StringComparison.OrdinalIgnoreCase))
                    {
                        installStarted = true;
                        onProgress?.Invoke(new TransferProgress(0, "Iniciando...", TransferType.Install));
                    }
                    else if (installStarted && (e.Data.Contains("Success", StringComparison.OrdinalIgnoreCase) || e.Data.Contains("Success", StringComparison.OrdinalIgnoreCase)))
                    {
                        onProgress?.Invoke(new TransferProgress(100, "Completado", TransferType.Install));
                    }
                    else if (installStarted)
                    {
                        // Indeterminate progress animation for install
                        onProgress?.Invoke(new TransferProgress(-1, "Instalando...", TransferType.Install));
                    }
                }
                else
                {
                    // Standard/Sideload: report actual progress
                    onProgress?.Invoke(new TransferProgress(percent ?? -1, speed, transferType));
                }
            }
            catch
            {
                // Parsing tolerante a fallos
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                onOutputReceived?.Invoke($"[ERROR] {e.Data}");
            }
        };

        process.Exited += (sender, e) =>
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(outputBuilder.ToString());
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"No se pudo iniciar ADB para transferencia. Argumentos: {arguments}");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout ?? DefaultTimeout, cancellationToken));
        if (completedTask == tcs.Task)
        {
            return await tcs.Task;
        }

        // Timeout
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch { }

        throw new TimeoutException($"Transferencia ADB excedió el tiempo límite ({timeout ?? DefaultTimeout})");
    }

    /// <summary>
    /// Escanea la red local buscando dispositivos activos usando Ping concurrente.
    /// Detecta automáticamente la subred local y escanea .1 a .99.
    /// </summary>
    public static async Task ScanLocalNetworkAsync(
        Action<string> onOutputReceived,
        CancellationToken cancellationToken = default)
    {
        string? localIp = GetLocalIPv4();
        if (string.IsNullOrEmpty(localIp))
        {
            onOutputReceived?.Invoke("[SCAN] ERROR: No se pudo detectar la IP local de la máquina.");
            return;
        }

        // Extraer la base de la subred (ej: 192.168.1.x)
        var ipParts = localIp.Split('.');
        if (ipParts.Length != 4)
        {
            onOutputReceived?.Invoke($"[SCAN] ERROR: Formato IP inesperado: {localIp}");
            return;
        }

        string subnetBase = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}";
        onOutputReceived?.Invoke($"[SCAN] Iniciando escaneo de red: {subnetBase}.1-99 (IP local: {localIp})");

        var pingTasks = new List<Task>();
        var activeIps = new System.Collections.Concurrent.ConcurrentBag<string>();
        int completed = 0;
        const int totalToScan = 99;
        var progressLock = new object();

        for (int i = 1; i <= 99; i++)
        {
            int ipSuffix = i;
            string targetIp = $"{subnetBase}.{ipSuffix}";
            
            // Saltar la propia IP
            if (targetIp == localIp) continue;

            var task = Task.Run(async () =>
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(targetIp, 500); // 500ms timeout por ping
                    
                    if (reply.Status == IPStatus.Success)
                    {
                        activeIps.Add(targetIp);
                        onOutputReceived?.Invoke($"[SCAN] ✓ Activo: {targetIp} ({reply.RoundtripTime}ms)");
                    }
                }
                catch (Exception)
                {
                    // Ignorar errores de ping individuales
                }
                finally
                {
                    lock (progressLock)
                    {
                        completed++;
                        if (completed % 20 == 0 || completed == totalToScan - 1)
                        {
                            onOutputReceived?.Invoke($"[SCAN] Progreso: {completed}/{totalToScan - 1} IPs verificadas...");
                        }
                    }
                }
            }, cancellationToken);

            pingTasks.Add(task);

            // Limitar concurrencia a 20 pings simultáneos para no saturar
            if (pingTasks.Count >= 20)
            {
                await Task.WhenAll(pingTasks);
                pingTasks.Clear();
            }
        }

        // Esperar los restantes
        if (pingTasks.Count > 0)
        {
            await Task.WhenAll(pingTasks);
        }

        var results = activeIps.OrderBy(ip => 
        {
            var parts = ip.Split('.');
            return int.TryParse(parts[3], out int n) ? n : 0;
        }).ToList();

        onOutputReceived?.Invoke($"[SCAN] Escaneo completado. {results.Count} dispositivos activos encontrados:");
        foreach (var ip in results)
        {
            onOutputReceived?.Invoke($"[SCAN]   → {ip}");
        }

        if (results.Count == 0)
        {
            onOutputReceived?.Invoke("[SCAN] No se encontraron dispositivos activos en el rango .1-.99");
            onOutputReceived?.Invoke("[SCAN] Sugerencia: Verifica que el dispositivo esté en la misma red y el depuración Wi-Fi/TCP/IP esté habilitado.");
        }
    }

    /// <summary>
    /// Escaneo mDNS nativo usando 'adb mdns services' (ADB 34+)
    /// Descubre dispositivos Android con depuración inalámbrica anunciada via mDNS.
    /// </summary>
    public static async Task ScanMdnsAsync(
        Action<string> onOutputReceived,
        CancellationToken cancellationToken = default)
    {
        onOutputReceived?.Invoke("[mDNS] Iniciando escaneo mDNS nativo (adb mdns services)...");
        
        try
        {
            var result = await ExecuteCommandAsync(
                "mdns services",
                onOutputReceived!,
                TimeSpan.FromSeconds(15),
                cancellationToken);

            // Parse mDNS output for device IPs and ports
            // Typical output format:
            // _adb-tls-pairing._tcp.local. → 192.168.1.100:5555
            // _adb-tls-connect._tcp.local. → 192.168.1.100:5556
            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var foundDevices = new List<(string Service, string HostPort)>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Look for IP:PORT patterns in mDNS output
                var match = System.Text.RegularExpressions.Regex.Match(
                    trimmed,
                    @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}):(\d{1,5})");
                
                if (match.Success)
                {
                    var ip = match.Groups[1].Value;
                    var port = match.Groups[2].Value;
                    var service = trimmed.Contains("_adb-tls-pairing") ? "Pairing" : 
                                  trimmed.Contains("_adb-tls-connect") ? "Connect" : "Unknown";
                    
                    foundDevices.Add((service, $"{ip}:{port}"));
                    onOutputReceived?.Invoke($"[mDNS] ✓ {service}: {ip}:{port}");
                }
                else
                {
                    // Still log the raw line for debugging
                    onOutputReceived?.Invoke($"[mDNS] {trimmed}");
                }
            }

            if (foundDevices.Count == 0)
            {
                onOutputReceived?.Invoke("[mDNS] No se encontraron servicios ADB via mDNS");
                onOutputReceived?.Invoke("[mDNS] Asegúrate de que el dispositivo tiene 'Depuración inalámbrica' activada y está en la misma red");
            }
            else
            {
                onOutputReceived?.Invoke($"[mDNS] Escaneo completado. {foundDevices.Count} servicio(s) encontrado(s).");
            }
        }
        catch (OperationCanceledException)
        {
            onOutputReceived?.Invoke("[mDNS] Escaneo cancelado");
        }
        catch (Exception ex)
        {
            onOutputReceived?.Invoke($"[mDNS] ERROR: {ex.Message}");
            onOutputReceived?.Invoke("[mDNS] Nota: Requiere ADB 34.0.0 o superior");
        }
    }

    private static string? GetLocalIPv4()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // Filtrar IPs de loopback y APIs virtuales (Docker, Hyper-V, etc.)
                    byte[] bytes = ip.GetAddressBytes();
                    if (bytes[0] == 127) continue; // 127.x.x.x loopback
                    if (bytes[0] == 169 && bytes[1] == 254) continue; // 169.254.x.x APIPA
                    return ip.ToString();
                }
            }
        }
        catch { }
        return null;
    }
}
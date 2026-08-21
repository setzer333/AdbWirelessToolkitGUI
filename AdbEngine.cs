using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace AdbWirelessToolkitGUI;

public static class AdbEngine
{
    private static readonly string AdbPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "PlatformTools",
        "adb.exe");

    public static async Task<string> ExecuteCommandAsync(
        string arguments,
        Action<string> onOutputReceived)
    {
        if (!File.Exists(AdbPath))
        {
            throw new FileNotFoundException($"ADB no encontrado en: {AdbPath}");
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
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = processStartInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var tcs = new TaskCompletionSource<string>();

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
            tcs.SetResult(outputBuilder.ToString());
        };

        process.EnableRaisingEvents = true;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return await tcs.Task;
    }

    public static async Task<string> ExecuteTransferAsync(
        string arguments,
        Action<string> onOutputReceived,
        Action<int> onProgress)
    {
        if (!File.Exists(AdbPath))
        {
            throw new FileNotFoundException($"ADB no encontrado en: {AdbPath}");
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
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = processStartInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var tcs = new TaskCompletionSource<string>();

        var progressRegex = new Regex(@"(\d+)%");

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                onOutputReceived?.Invoke(e.Data);

                var match = progressRegex.Match(e.Data);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int percent))
                {
                    onProgress?.Invoke(Math.Clamp(percent, 0, 100));
                }
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
            tcs.SetResult(outputBuilder.ToString());
        };

        process.EnableRaisingEvents = true;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return await tcs.Task;
    }
}
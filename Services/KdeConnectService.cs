using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace h_sms_sender.Services;

public class KdeDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Reachable { get; set; }
    public bool Trusted { get; set; }

    public override string ToString() =>
        $"{Name} [{Id}]" + (Reachable ? "" : " (오프라인)");
}

public class SendResult
{
    public string RecipientName { get; set; } = "";
    public string Phone { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class KdeConnectService
{
    private readonly string _cliPath;

    public KdeConnectService(string cliPath)
    {
        _cliPath = cliPath;
    }

    public bool CliExists() => File.Exists(_cliPath);

    private static readonly string[] CommonCliPaths = new[]
    {
        @"C:\Program Files\KDE Connect\bin\kdeconnect-cli.exe",
        @"C:\Program Files (x86)\KDE Connect\bin\kdeconnect-cli.exe",
        @"C:\Program Files\KDE\bin\kdeconnect-cli.exe",
    };

    public static string? AutoDetectCliPath()
    {
        foreach (var p in CommonCliPaths)
            if (File.Exists(p)) return p;
        return null;
    }

    public async Task<List<KdeDevice>> ListDevicesAsync()
    {
        // -a: list paired & reachable; --id-name-only gives "<id> <name>"
        var (ok, stdout, stderr) = await RunAsync("-a --id-name-only");
        var devices = new List<KdeDevice>();
        if (ok)
        {
            foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                var idx = trimmed.IndexOf(' ');
                if (idx <= 0) continue;
                devices.Add(new KdeDevice
                {
                    Id = trimmed[..idx],
                    Name = trimmed[(idx + 1)..].Trim(),
                    Reachable = true,
                    Trusted = true
                });
            }
        }

        // also include paired-but-not-reachable
        var (ok2, stdout2, _) = await RunAsync("-l");
        if (ok2)
        {
            foreach (var line in stdout2.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                // format roughly: "- <name>: <id> (paired and reachable)" etc.
                var m = Regex.Match(line, @"-\s+(.+?):\s+(\S+)\s+\((.+)\)");
                if (!m.Success) continue;
                var name = m.Groups[1].Value.Trim();
                var id = m.Groups[2].Value.Trim();
                var status = m.Groups[3].Value.ToLowerInvariant();
                if (devices.Any(d => d.Id == id)) continue;
                devices.Add(new KdeDevice
                {
                    Id = id,
                    Name = name,
                    Reachable = status.Contains("reachable"),
                    Trusted = status.Contains("paired") || status.Contains("trusted")
                });
            }
        }

        return devices;
    }

    public async Task PingAsync(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId)) return;
        try { await RunAsync($"-d \"{deviceId}\" --ping"); } catch { }
    }

    public async Task<SendResult> SendSmsAsync(string deviceId, string recipientName, string phone, string body)
    {
        var args = $"-d \"{deviceId}\" --send-sms \"{Escape(body)}\" --destination \"{phone}\"";
        var (ok, stdout, stderr) = await RunAsync(args);
        return new SendResult
        {
            RecipientName = recipientName,
            Phone = phone,
            Success = ok && string.IsNullOrWhiteSpace(stderr),
            Message = ok ? (stdout.Trim().Length > 0 ? stdout.Trim() : "전송됨") : stderr.Trim()
        };
    }

    private static string Escape(string s) => s.Replace("\"", "\\\"");

    private async Task<(bool ok, string stdout, string stderr)> RunAsync(string args)
    {
        if (!CliExists())
            return (false, "", $"kdeconnect-cli.exe를 찾을 수 없습니다: {_cliPath}");

        var psi = new ProcessStartInfo
        {
            FileName = _cliPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using var proc = Process.Start(psi)!;
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return (proc.ExitCode == 0, await stdoutTask, await stderrTask);
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }
}

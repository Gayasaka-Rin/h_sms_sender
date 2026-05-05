using System.Text.Json;
using h_sms_sender.Models;

namespace h_sms_sender.Services;

public static class Storage
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string AppDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "h_sms_sender");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    private static string ConfigPath => Path.Combine(AppDir, "config.json");
    private static string HistoryPath => Path.Combine(AppDir, "history.json");

    public static AppConfig LoadConfig()
    {
        AppConfig cfg;
        if (!File.Exists(ConfigPath))
        {
            cfg = new AppConfig();
        }
        else
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                cfg = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                cfg = new AppConfig();
            }
        }

        // 저장된 CLI 경로가 실제로 없으면 일반적인 위치들을 자동 탐색해서 채워줌
        if (!File.Exists(cfg.KdeConnectCliPath))
        {
            var detected = KdeConnectService.AutoDetectCliPath();
            if (detected != null) cfg.KdeConnectCliPath = detected;
        }

        SaveConfig(cfg);
        return cfg;
    }

    public static void SaveConfig(AppConfig cfg)
    {
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(cfg, JsonOpts));
    }

    public static List<HistoryEntry> LoadHistory()
    {
        if (!File.Exists(HistoryPath)) return new List<HistoryEntry>();
        try
        {
            var json = File.ReadAllText(HistoryPath);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new List<HistoryEntry>();
        }
        catch
        {
            return new List<HistoryEntry>();
        }
    }

    public static void SaveHistory(List<HistoryEntry> entries)
    {
        File.WriteAllText(HistoryPath, JsonSerializer.Serialize(entries, JsonOpts));
    }

    public static void AppendHistory(HistoryEntry entry)
    {
        var list = LoadHistory();
        list.Insert(0, entry);
        SaveHistory(list);
    }
}

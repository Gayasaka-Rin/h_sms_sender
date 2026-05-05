namespace h_sms_sender.Models;

public class HistoryEntry
{
    public DateTime Timestamp { get; set; }
    public List<string> RecipientNames { get; set; } = new();
    public List<string> RecipientPhones { get; set; } = new();
    public string Body { get; set; } = "";
    public List<string> Failures { get; set; } = new();

    public string FirstLine
    {
        get
        {
            var line = (Body ?? "").Split('\n').FirstOrDefault()?.Trim() ?? "";
            return line.Length > 60 ? line[..60] + "…" : line;
        }
    }

    public string DisplayLabel => $"{Timestamp:yyyy-MM-dd HH:mm}  {FirstLine}";
}

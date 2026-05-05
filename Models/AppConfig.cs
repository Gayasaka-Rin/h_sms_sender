namespace h_sms_sender.Models;

public class AppConfig
{
    public string KdeConnectCliPath { get; set; } = @"C:\Program Files\KDE Connect\bin\kdeconnect-cli.exe";
    public string DeviceId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public FontConfig Font { get; set; } = new();
    public List<Recipient> Recipients { get; set; } = new();
}

public class FontConfig
{
    public string Family { get; set; } = "맑은 고딕";
    public float Size { get; set; } = 16f;
    public bool Bold { get; set; } = false;
    public bool Italic { get; set; } = false;
}

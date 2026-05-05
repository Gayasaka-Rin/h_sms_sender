namespace h_sms_sender.Models;

public class Recipient
{
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";

    public override string ToString() => $"{Name} ({Phone})";
}

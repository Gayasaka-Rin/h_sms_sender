namespace h_sms_sender;

static class Program
{
    [STAThread]
    static void Main()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "h_sms_sender_crash.log");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try { File.AppendAllText(logPath, $"[{DateTime.Now:O}] UNHANDLED:\n{e.ExceptionObject}\n\n"); } catch { }
        };
        Application.ThreadException += (_, e) =>
        {
            try { File.AppendAllText(logPath, $"[{DateTime.Now:O}] THREAD:\n{e.Exception}\n\n"); } catch { }
        };

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(logPath, $"[{DateTime.Now:O}] MAIN:\n{ex}\n\n"); } catch { }
            throw;
        }
    }
}


namespace KiraTakip.Models.Settings;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";
    public string From { get; set; } = "";
    public string FromName { get; set; } = "KiraTakip";
    public bool UseStartTls { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}

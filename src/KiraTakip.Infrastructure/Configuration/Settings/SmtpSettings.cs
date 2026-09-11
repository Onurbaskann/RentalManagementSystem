namespace KiraTakip.Models.Settings;

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromName { get; set; } = "KiraTakip";
    public bool UseStartTls { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
}

namespace KiraTakip.Models.Settings;

public class LeaseExpirationSettings
{
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
}

namespace KiraTakip.Models.Settings;

public class ReservationCompletionSettings
{
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 5;
    public int BatchSize { get; set; } = 50;
}

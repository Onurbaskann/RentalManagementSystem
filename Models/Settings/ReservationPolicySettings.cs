namespace KiraTakip.Models.Settings;

public class ReservationPolicySettings
{
    public const string TimeZoneId = "Turkey Standard Time";

    public int MinimumDurationMinutes { get; set; } = 15;
    public int MaximumDurationMinutes { get; set; } = 1440;
    public int MinimumAdvanceMinutes { get; set; } = 0;
    public int MaximumAdvanceDays { get; set; } = 365;
    public int ModificationCutoffMinutes { get; set; } = 120;
    public int CompletionGraceMinutes { get; set; } = 15;
    public int MaximumAttendeeCount { get; set; } = 100;
}

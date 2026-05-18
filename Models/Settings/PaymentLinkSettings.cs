namespace KiraTakip.Models.Settings;

public class PaymentLinkSettings
{
    public string BaseUrl { get; set; } = "https://localhost:5031";
    public string Secret { get; set; } = "";
    public int TokenTtlHours { get; set; } = 168;
    public int ReminderDaysBefore { get; set; } = 5;
    public int ReminderCooldownDays { get; set; } = 7;
}

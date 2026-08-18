namespace KiraTakip.Models.Settings;

public class PaymentLinkSettings
{
    public string BaseUrl { get; set; } = "https://localhost:5031";
    public string Secret { get; set; } = string.Empty;
}

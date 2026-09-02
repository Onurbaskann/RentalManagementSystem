namespace KiraTakip.Models.Settings;

/// <summary>
/// Paratika sanal POS ortam ayarları. Diğer ayar sınıfları (SmtpSettings, SecureTokenSettings)
/// gibi düz POCO — doğrulama açılışta değil, kullanıldığı yerde (İç Faz 7,
/// ParatikaOnlinePaymentProvider) Guard ile yapılır. İç Faz 6'da hiçbir HTTP çağrısında
/// kullanılmaz, yalnız config iskeleti olarak eklenmiştir.
/// </summary>
public class ParatikaOptions
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string HostedPaymentPageBaseUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotificationUrl { get; set; } = string.Empty;
    public int SessionExpiryMinutes { get; set; } = 15;
    public int HttpTimeoutSeconds { get; set; } = 30;
}

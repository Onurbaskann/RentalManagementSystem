using System.Security.Cryptography;
using System.Text;
using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KiraTakip.Services;

public class PaymentLinkService : IPaymentLinkService
{
    private readonly PaymentLinkSettings _settings;

    public PaymentLinkService(IOptions<PaymentLinkSettings> options)
    {
        _settings = options.Value;
    }

    public string BuildLink(int kiraciId)
    {
        var expiresUnix = DateTimeOffset.UtcNow.AddHours(_settings.TokenTtlHours).ToUnixTimeSeconds();
        var token = BuildToken(kiraciId, expiresUnix);
        return $"{_settings.BaseUrl.TrimEnd('/')}/Odeme/Portal?t={Uri.EscapeDataString(token)}";
    }

    public bool TryValidate(string token, out int kiraciId, out string? reason)
    {
        kiraciId = 0;
        reason = null;
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            reason = "Geçersiz token formatı.";
            return false;
        }

        if (!long.TryParse(parts[0], out var expiresUnix) || !int.TryParse(parts[1], out kiraciId))
        {
            reason = "Geçersiz token formatı.";
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnix)
        {
            reason = "Ödeme linkinin süresi dolmuştur.";
            return false;
        }

        var expected = BuildToken(kiraciId, expiresUnix);
        var expectedParts = expected.Split('.');
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(parts[2]),
                Encoding.UTF8.GetBytes(expectedParts[2])))
        {
            reason = "Geçersiz veya değiştirilmiş token.";
            return false;
        }

        return true;
    }

    private string BuildToken(int kiraciId, long expiresUnix)
    {
        var plaintext = $"{kiraciId}|{expiresUnix}|payment-portal";
        var keyBytes = Encoding.UTF8.GetBytes(_settings.Secret.PadRight(32, '0'));
        var hmac = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(plaintext));
        var hmacBase64Url = Convert.ToBase64String(hmac)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"{expiresUnix}.{kiraciId}.{hmacBase64Url}";
    }
}

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

    public string BuildLink(int tahakkukId)
    {
        var expiresUnix = DateTimeOffset.UtcNow.AddHours(_settings.TokenTtlHours).ToUnixTimeSeconds();
        var token = BuildToken(tahakkukId, expiresUnix);
        return $"{_settings.BaseUrl.TrimEnd('/')}/Odeme/Portal/{tahakkukId}?t={Uri.EscapeDataString(token)}";
    }

    public bool TryValidate(int tahakkukId, string token, out string? reason)
    {
        reason = null;
        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            reason = "Geçersiz token formatı.";
            return false;
        }

        if (!long.TryParse(parts[0], out var expiresUnix))
        {
            reason = "Geçersiz token formatı.";
            return false;
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUnix)
        {
            reason = "Ödeme linkinin süresi dolmuştur.";
            return false;
        }

        var expected = BuildToken(tahakkukId, expiresUnix);
        var expectedParts = expected.Split('.');
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(parts[1]),
                Encoding.UTF8.GetBytes(expectedParts[1])))
        {
            reason = "Geçersiz veya değiştirilmiş token.";
            return false;
        }

        return true;
    }

    private string BuildToken(int tahakkukId, long expiresUnix)
    {
        var plaintext = $"{tahakkukId}|{expiresUnix}";
        var keyBytes = Encoding.UTF8.GetBytes(_settings.Secret.PadRight(32, '0'));
        var hmac = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(plaintext));
        var hmacBase64Url = Convert.ToBase64String(hmac)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"{expiresUnix}.{hmacBase64Url}";
    }
}

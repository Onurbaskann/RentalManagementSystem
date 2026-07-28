using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace KiraTakip.Services;

public class SecureTokenService : ISecureTokenService
{
    private readonly byte[] _secretBytes;

    public SecureTokenService(IOptions<SecureTokenSettings> options)
    {
        var secret = options.Value.Secret;
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            throw new InvalidOperationException("SecureToken:Secret yapılandırılmamış veya çok kısa (en az 32 karakter).");
        _secretBytes = Encoding.UTF8.GetBytes(secret);
    }

    public SecureTokenResult Generate(string entityId, string purpose, TimeSpan ttl)
    {
        var expiresAt = DateTime.UtcNow.Add(ttl);
        var expiresUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds().ToString();

        var message = $"{entityId}|{expiresUnix}|{purpose}";
        var hmac = ComputeHmac(message);

        var raw = $"{B64(entityId)}.{B64(expiresUnix)}.{B64(hmac)}";
        var hash = ComputeHash(raw);

        return new SecureTokenResult(raw, hash, expiresAt);
    }

    public bool TryValidate(string rawToken, string entityId, string purpose, out string? reason)
    {
        reason = null;
        try
        {
            var parts = rawToken.Split('.');
            if (parts.Length != 3) { reason = "Geçersiz bağlantı biçimi."; return false; }

            var decodedId = FromB64(parts[0]);
            var decodedExpires = FromB64(parts[1]);
            var decodedHmac = FromB64(parts[2]);

            if (decodedId != entityId) { reason = "Bağlantı kimliği uyuşmuyor."; return false; }

            if (!long.TryParse(decodedExpires, out var expiresUnix))
            { reason = "Geçersiz son kullanma tarihi."; return false; }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresUnix).UtcDateTime;
            if (DateTime.UtcNow > expiresAt) { reason = "Bağlantının süresi dolmuş."; return false; }

            var expected = ComputeHmac($"{entityId}|{decodedExpires}|{purpose}");
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(decodedHmac),
                    Encoding.UTF8.GetBytes(expected)))
            { reason = "İmza geçersiz."; return false; }

            return true;
        }
        catch
        {
            reason = "Bağlantı doğrulanamadı.";
            return false;
        }
    }

    public string ComputeHash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string ComputeHmac(string message)
    {
        var msgBytes = Encoding.UTF8.GetBytes(message);
        var hash = HMACSHA256.HashData(_secretBytes, msgBytes);
        return Convert.ToBase64String(hash);
    }

    private static string B64(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string FromB64(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}

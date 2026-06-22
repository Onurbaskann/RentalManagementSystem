namespace KiraTakip.Services.Interfaces;

public record SecureTokenResult(string RawToken, string TokenHash, DateTime ExpiresAt);

public interface ISecureTokenService
{
    SecureTokenResult Generate(string entityId, string purpose, TimeSpan ttl);
    bool TryValidate(string rawToken, string entityId, string purpose, out string? reason);
    string ComputeHash(string rawToken);
}

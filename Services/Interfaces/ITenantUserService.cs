namespace KiraTakip.Services.Interfaces;

public interface ITenantUserService
{
    Task<bool> HasSonYetkiliAsync(int tenantId, string? excludeUserId = null, int? excludeRolId = null, CancellationToken ct = default);
    Task EnsureSonYetkiliAsync(int tenantId, string? excludeUserId = null, int? excludeRolId = null, CancellationToken ct = default);
}

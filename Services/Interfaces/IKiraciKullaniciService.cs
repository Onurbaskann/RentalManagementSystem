namespace KiraTakip.Services.Interfaces;

public interface IKiraciKullaniciService
{
    Task<bool> HasSonYetkiliAsync(int kiraciId, string? excludeUserId = null, int? excludeRolId = null, CancellationToken ct = default);
    Task EnsureSonYetkiliAsync(int kiraciId, string? excludeUserId = null, int? excludeRolId = null, CancellationToken ct = default);
}

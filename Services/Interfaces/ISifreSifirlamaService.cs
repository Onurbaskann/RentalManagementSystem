using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface ISifreSifirlamaService
{
    Task<bool> TalepOlusturAsync(string email, string? ipAddress, CancellationToken ct = default);
    Task<(bool Success, string? Error, SifreSifirlamaTalebi? Talep)> DogrulaAsync(string rawToken, CancellationToken ct = default);
    Task<bool> SifreDegistirAsync(SifreSifirlamaTalebi talep, string yeniSifre, CancellationToken ct = default);
}

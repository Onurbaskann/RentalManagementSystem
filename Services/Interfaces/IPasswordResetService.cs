using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IPasswordResetService
{
    Task<bool> TalepOlusturAsync(string email, string? ipAddress, CancellationToken ct = default);
    Task<(bool Success, string? Error, PasswordResetRequest? Talep)> DogrulaAsync(string rawToken, CancellationToken ct = default);
    Task<bool> SifreDegistirAsync(PasswordResetRequest talep, string yeniSifre, CancellationToken ct = default);
}

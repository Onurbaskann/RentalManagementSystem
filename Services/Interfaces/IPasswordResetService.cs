using KiraTakip.Models.Dtos.PasswordReset;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IPasswordResetService
{
    Task<bool> RequestAsync(RequestInput input, CancellationToken ct = default);
    Task<(bool Success, string? Error, PasswordResetRequest? Talep)> ValidateAsync(string token, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(PasswordResetRequest request, ResetPasswordInput input, CancellationToken ct = default);
}

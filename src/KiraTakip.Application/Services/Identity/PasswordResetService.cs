using KiraTakip.Data;
using KiraTakip.Domain.Identity;
using KiraTakip.Models.Dtos.PasswordReset;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Notifications;
using KiraTakip.Common;
using Microsoft.Extensions.Logging;

namespace KiraTakip.Services.Identity;

public class PasswordResetService(
    IPasswordResetRequestRepository passwordResetRequestRepository,
    IUnitOfWork unitOfWork,
    ISecureTokenService tokenService,
    IMailService mailService,
    IRazorViewToStringRenderer renderer,
    IRequestContext requestContext,
    IAuditService auditService,
    IApplicationUserManager userManager,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    private const string Purpose = "password-reset";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    private const int RateLimitMaxRequests = 3;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

    public async Task<bool> RequestAsync(RequestInput input, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(input.Email.Trim());
        // Kullanıcı yoksa bile başarılı dön (user enumeration önleme)
        if (user is null || !user.IsActive)
            return true;

        var rateLimitCutoff = DateTime.UtcNow.Subtract(RateLimitWindow);
        var recentCount = await passwordResetRequestRepository.CountRecentPendingAsync(user.Id, rateLimitCutoff, ct);

        if (PasswordResetLifecyclePolicy.IsRateLimitExceeded(recentCount, RateLimitMaxRequests))
        {
            logger.LogWarning("Şifre sıfırlama rate limit: {Email}", input.Email);
            return true;
        }

        var talep = new PasswordResetRequest
        {
            UserId = user.Id,
            RequestIp = input.IpAddress,
            ExpiresAt = DateTime.UtcNow.Add(Ttl),
            Status = PasswordResetStatus.Pending,
        };

        await passwordResetRequestRepository.AddAsync(talep);
        await unitOfWork.SaveChangesAsync(ct);

        var tokenResult = tokenService.Generate(talep.Id.ToString(), Purpose, Ttl);
        talep.TokenHash = tokenResult.TokenHash;
        talep.ExpiresAt = tokenResult.ExpiresAt;
        await unitOfWork.SaveChangesAsync(ct);

        await MailGonderAsync(user, tokenResult.RawToken, ct);
        await auditService.LogAsync("User.PasswordReset.Requested", "PasswordResetRequest", talep.Id.ToString(), user.Id);
        return true;
    }

    public async Task<(bool Success, string? Error, PasswordResetRequest? Talep)> ValidateAsync(string token, CancellationToken ct = default)
    {
        var hash = tokenService.ComputeHash(token);
        var talep = await passwordResetRequestRepository.GetByTokenHashIgnoringFiltersAsync(hash, ct);

        if (talep is null)
            return (false, "Şifre sıfırlama linki geçersiz.", null);

        var validationResult = PasswordResetLifecyclePolicy.Validate(talep.Status, talep.ExpiresAt, DateTime.UtcNow);
        if (validationResult == PasswordResetValidationResult.Used)
            return (false, "Bu link daha önce kullanılmış.", null);

        if (validationResult == PasswordResetValidationResult.Cancelled)
            return (false, "Bu link iptal edilmiş.", null);

        if (validationResult == PasswordResetValidationResult.Expired)
        {
            talep.Status = PasswordResetStatus.Expired;
            await unitOfWork.SaveChangesAsync(ct);
            return (false, "Şifre sıfırlama linkinin süresi dolmuş. Yeni talep oluşturun.", null);
        }

        if (!tokenService.TryValidate(token, talep.Id.ToString(), Purpose, out var reason))
            return (false, reason ?? "Token doğrulanamadı.", null);

        return (true, null, talep);
    }

    public async Task<bool> ResetPasswordAsync(PasswordResetRequest request, ResetPasswordInput input, CancellationToken ct = default)
    {
        var talep = request;
        var user = await userManager.FindByIdAsync(talep.UserId);
        if (user is null) return false;

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, identityToken, input.NewPassword);
        if (!result.Succeeded) return false;

        talep.Status = PasswordResetStatus.Used;
        talep.UsedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

        await userManager.UpdateSecurityStampAsync(user);
        await auditService.LogAsync("User.PasswordReset.Completed", "PasswordResetRequest", talep.Id.ToString(), user.Id);
        return true;
    }

    private async Task MailGonderAsync(ApplicationUser user, string rawToken, CancellationToken ct)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(requestContext.BaseUrl)
            ? requestContext.BaseUrl
            : "http://localhost:5031";

        var link = $"{baseUrl}/Account/ResetPassword?token={Uri.EscapeDataString(rawToken)}";

        var model = new SifreSifirlamaMailModel
        {
            AdSoyad = user.AdSoyad ?? user.Email ?? user.Id,
            SifirlaLink = link,
            SonTarih = DateTime.UtcNow.Add(Ttl).ToLocalTime()
        };

        string html;
        try
        {
            html = await renderer.RenderAsync("/Views/Shared/EmailTemplates/SifreSifirlama.cshtml", model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Şifre sıfırlama mail template render hatası");
            throw;
        }

        await mailService.SendAsync(user.Email!, user.AdSoyad ?? user.Email!, "KiraTakip — Şifre Sıfırlama", html, ct);
    }
}

public class SifreSifirlamaMailModel
{
    public string AdSoyad { get; set; } = string.Empty;
    public string SifirlaLink { get; set; } = string.Empty;
    public DateTime SonTarih { get; set; }
}

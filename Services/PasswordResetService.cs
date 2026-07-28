using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos.PasswordReset;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IPasswordResetRequestRepository _passwordResetRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenService _tokenService;
    private readonly IMailService _mailService;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PasswordResetService> _logger;

    private const string Purpose = "password-reset";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    private const int RateLimitMaxRequests = 3;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

    public PasswordResetService(
        IPasswordResetRequestRepository passwordResetRequestRepository,
        IUnitOfWork unitOfWork,
        ISecureTokenService tokenService,
        IMailService mailService,
        IRazorViewToStringRenderer renderer,
        IHttpContextAccessor httpContextAccessor,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        ILogger<PasswordResetService> logger)
    {
        _passwordResetRequestRepository = passwordResetRequestRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _mailService = mailService;
        _renderer = renderer;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> RequestAsync(RequestInput input, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(input.Email.Trim());
        // Kullanıcı yoksa bile başarılı dön (user enumeration önleme)
        if (user is null || !user.IsActive)
            return true;

        var rateLimitCutoff = DateTime.UtcNow.Subtract(RateLimitWindow);
        var recentCount = await _passwordResetRequestRepository.CountRecentPendingAsync(user.Id, rateLimitCutoff, ct);

        if (recentCount >= RateLimitMaxRequests)
        {
            _logger.LogWarning("Şifre sıfırlama rate limit: {Email}", input.Email);
            return true;
        }

        var talep = new PasswordResetRequest
        {
            UserId = user.Id,
            RequestIp = input.IpAddress,
            ExpiresAt = DateTime.UtcNow.Add(Ttl),
            Status = PasswordResetStatus.Pending,
        };

        await _passwordResetRequestRepository.AddAsync(talep);
        await _unitOfWork.SaveChangesAsync(ct);

        var tokenResult = _tokenService.Generate(talep.Id.ToString(), Purpose, Ttl);
        talep.TokenHash = tokenResult.TokenHash;
        talep.ExpiresAt = tokenResult.ExpiresAt;
        await _unitOfWork.SaveChangesAsync(ct);

        await MailGonderAsync(user, tokenResult.RawToken, ct);
        await _auditService.LogAsync("User.PasswordReset.Requested", "PasswordResetRequest", talep.Id.ToString(), user.Id);
        return true;
    }

    public async Task<(bool Success, string? Error, PasswordResetRequest? Talep)> ValidateAsync(string token, CancellationToken ct = default)
    {
        var hash = _tokenService.ComputeHash(token);
        var talep = await _passwordResetRequestRepository.GetByTokenHashIgnoringFiltersAsync(hash, ct);

        if (talep is null)
            return (false, "Şifre sıfırlama linki geçersiz.", null);

        if (talep.Status == PasswordResetStatus.Used)
            return (false, "Bu link daha önce kullanılmış.", null);

        if (talep.Status == PasswordResetStatus.Cancelled)
            return (false, "Bu link iptal edilmiş.", null);

        if (talep.ExpiresAt < DateTime.UtcNow)
        {
            talep.Status = PasswordResetStatus.Expired;
            await _unitOfWork.SaveChangesAsync(ct);
            return (false, "Şifre sıfırlama linkinin süresi dolmuş. Yeni talep oluşturun.", null);
        }

        if (!_tokenService.TryValidate(token, talep.Id.ToString(), Purpose, out var reason))
            return (false, reason ?? "Token doğrulanamadı.", null);

        return (true, null, talep);
    }

    public async Task<bool> ResetPasswordAsync(PasswordResetRequest request, ResetPasswordInput input, CancellationToken ct = default)
    {
        var talep = request;
        var user = await _userManager.FindByIdAsync(talep.UserId);
        if (user is null) return false;

        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityToken, input.NewPassword);
        if (!result.Succeeded) return false;

        talep.Status = PasswordResetStatus.Used;
        talep.UsedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        await _userManager.UpdateSecurityStampAsync(user);
        await _auditService.LogAsync("User.PasswordReset.Completed", "PasswordResetRequest", talep.Id.ToString(), user.Id);
        return true;
    }

    private async Task MailGonderAsync(ApplicationUser user, string rawToken, CancellationToken ct)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is not null
            ? $"{request.Scheme}://{request.Host}"
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
            html = await _renderer.RenderAsync("/Views/Shared/EmailTemplates/SifreSifirlama.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Şifre sıfırlama mail template render hatası");
            throw;
        }

        await _mailService.SendAsync(user.Email!, user.AdSoyad ?? user.Email!, "KiraTakip — Şifre Sıfırlama", html, ct);
    }
}

public class SifreSifirlamaMailModel
{
    public string AdSoyad { get; set; } = string.Empty;
    public string SifirlaLink { get; set; } = string.Empty;
    public DateTime SonTarih { get; set; }
}

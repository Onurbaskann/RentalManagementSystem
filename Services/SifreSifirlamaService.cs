using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class SifreSifirlamaService : ISifreSifirlamaService
{
    private readonly ApplicationDbContext _db;
    private readonly ISecureTokenService _tokenService;
    private readonly IMailService _mailService;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SifreSifirlamaService> _logger;

    private const string Purpose = "password-reset";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    private const int RateLimitMaxRequests = 3;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

    public SifreSifirlamaService(
        ApplicationDbContext db,
        ISecureTokenService tokenService,
        IMailService mailService,
        IRazorViewToStringRenderer renderer,
        IHttpContextAccessor httpContextAccessor,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        ILogger<SifreSifirlamaService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _mailService = mailService;
        _renderer = renderer;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<bool> TalepOlusturAsync(string email, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        // Kullanıcı yoksa bile başarılı dön (user enumeration önleme)
        if (user is null || !user.IsActive)
            return true;

        var rateLimitCutoff = DateTime.UtcNow.Subtract(RateLimitWindow);
        var recentCount = await _db.SifreSifirlamaTalepleri
            .CountAsync(t => t.UserId == user.Id
                          && t.Durum == PasswordResetStatus.Pending
                          && t.CreatedAt >= rateLimitCutoff, ct);

        if (recentCount >= RateLimitMaxRequests)
        {
            _logger.LogWarning("Şifre sıfırlama rate limit: {Email}", email);
            return true;
        }

        var talep = new SifreSifirlamaTalebi
        {
            UserId = user.Id,
            TalepEdenIp = ipAddress,
            ExpiresAt = DateTime.UtcNow.Add(Ttl),
            Durum = PasswordResetStatus.Pending,
        };

        _db.SifreSifirlamaTalepleri.Add(talep);
        await _db.SaveChangesAsync(ct);

        var tokenResult = _tokenService.Generate(talep.Id.ToString(), Purpose, Ttl);
        talep.TokenHash = tokenResult.TokenHash;
        talep.ExpiresAt = tokenResult.ExpiresAt;
        await _db.SaveChangesAsync(ct);

        await MailGonderAsync(user, tokenResult.RawToken, ct);
        await _auditService.LogAsync("User.PasswordReset.Requested", "SifreSifirlamaTalebi", talep.Id.ToString(), user.Id);
        return true;
    }

    public async Task<(bool Success, string? Error, SifreSifirlamaTalebi? Talep)> DogrulaAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = _tokenService.ComputeHash(rawToken);
        var talep = await _db.SifreSifirlamaTalepleri
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (talep is null)
            return (false, "Şifre sıfırlama linki geçersiz.", null);

        if (talep.Durum == PasswordResetStatus.Used)
            return (false, "Bu link daha önce kullanılmış.", null);

        if (talep.Durum == PasswordResetStatus.Cancelled)
            return (false, "Bu link iptal edilmiş.", null);

        if (talep.ExpiresAt < DateTime.UtcNow)
        {
            talep.Durum = PasswordResetStatus.Expired;
            await _db.SaveChangesAsync(ct);
            return (false, "Şifre sıfırlama linkinin süresi dolmuş. Yeni talep oluşturun.", null);
        }

        if (!_tokenService.TryValidate(rawToken, talep.Id.ToString(), Purpose, out var reason))
            return (false, reason ?? "Token doğrulanamadı.", null);

        return (true, null, talep);
    }

    public async Task<bool> SifreDegistirAsync(SifreSifirlamaTalebi talep, string yeniSifre, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(talep.UserId);
        if (user is null) return false;

        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityToken, yeniSifre);
        if (!result.Succeeded) return false;

        talep.Durum = PasswordResetStatus.Used;
        talep.KullanmaTarihi = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _userManager.UpdateSecurityStampAsync(user);
        await _auditService.LogAsync("User.PasswordReset.Completed", "SifreSifirlamaTalebi", talep.Id.ToString(), user.Id);
        return true;
    }

    private async Task MailGonderAsync(ApplicationUser user, string rawToken, CancellationToken ct)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is not null
            ? $"{request.Scheme}://{request.Host}"
            : "http://localhost:5031";

        var link = $"{baseUrl}/Account/SifreSifirla?token={Uri.EscapeDataString(rawToken)}";

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

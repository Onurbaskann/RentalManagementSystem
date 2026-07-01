using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class DavetiyeService : IDavetiyeService, ITransactionalService
{
    private readonly ApplicationDbContext _db;
    private readonly ISecureTokenService _tokenService;
    private readonly IMailService _mailService;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRolService _userRolService;
    private readonly ILogger<DavetiyeService> _logger;

    private const string Purpose = "invite";
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    public DavetiyeService(
        ApplicationDbContext db,
        ISecureTokenService tokenService,
        IMailService mailService,
        IRazorViewToStringRenderer renderer,
        IHttpContextAccessor httpContextAccessor,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        IUserRolService userRolService,
        ILogger<DavetiyeService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _mailService = mailService;
        _renderer = renderer;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _userManager = userManager;
        _userRolService = userRolService;
        _logger = logger;
    }

    public async Task<Davetiye> GonderAsync(string email, string? adSoyad, int rolId, string davetEdenUserId, int? kiraciId = null, bool tumTasinmazlaraErisim = false, List<int>? tasinmazIds = null, List<int>? birimIds = null, CancellationToken ct = default)
    {
        var userType = kiraciId.HasValue ? UserType.Kiraci : UserType.Internal;
        var davetiye = new Davetiye
        {
            Email = email.Trim().ToLowerInvariant(),
            AdSoyad = adSoyad,
            RolId = rolId,
            DavetEdenUserId = davetEdenUserId,
            UserType = userType,
            KiraciId = kiraciId,
            TumTasinmazlaraErisim = tumTasinmazlaraErisim,
            TasinmazIds = (tasinmazIds != null && tasinmazIds.Any())
                ? System.Text.Json.JsonSerializer.Serialize(tasinmazIds)
                : null,
            BirimIds = (birimIds != null && birimIds.Any())
                ? System.Text.Json.JsonSerializer.Serialize(birimIds)
                : null,
            ExpiresAt = DateTime.UtcNow.Add(Ttl),
            Durum = DavetiyeDurum.Beklemede,
        };

        _db.Davetiyeler.Add(davetiye);
        await _db.SaveChangesAsync(ct);

        var tokenResult = _tokenService.Generate(davetiye.Id.ToString(), Purpose, Ttl);
        davetiye.TokenHash = tokenResult.TokenHash;
        davetiye.ExpiresAt = tokenResult.ExpiresAt;
        await _db.SaveChangesAsync(ct);

        await MailGonderAsync(davetiye, tokenResult.RawToken, ct);

        await _auditService.LogAsync("Invite.Sent", "Davetiye", davetiye.Id.ToString(), email);
        return davetiye;
    }

    public async Task<(bool Success, string? Error, Davetiye? Davetiye)> DogrulaAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = _tokenService.ComputeHash(rawToken);
        var davetiye = await _db.Davetiyeler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.TokenHash == hash, ct);

        if (davetiye is null)
            return (false, "Davet linki geçersiz.", null);

        if (davetiye.Durum == DavetiyeDurum.IptalEdildi)
            return (false, "Bu davet iptal edilmiş.", null);

        if (davetiye.Durum == DavetiyeDurum.KabulEdildi)
            return (false, "Bu davet daha önce kullanılmış.", null);

        if (davetiye.ExpiresAt < DateTime.UtcNow)
        {
            davetiye.Durum = DavetiyeDurum.SuresiDolmus;
            await _db.SaveChangesAsync(ct);
            return (false, "Davet linkinin süresi dolmuş. Yeni davet talep edin.", null);
        }

        if (!_tokenService.TryValidate(rawToken, davetiye.Id.ToString(), Purpose, out var reason))
            return (false, reason ?? "Token doğrulanamadı.", null);

        return (true, null, davetiye);
    }

    public async Task<ApplicationUser> KabulEtAsync(Davetiye davetiye, string adSoyad, string password, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = davetiye.Email,
            Email = davetiye.Email,
            AdSoyad = adSoyad,
            EmailConfirmed = true,
            UserType = davetiye.UserType,
            KiraciId = davetiye.KiraciId,
            TumTasinmazlaraErisim = davetiye.TumTasinmazlaraErisim,
            IsActive = true,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userRolService.AddRoleByRolIdAsync(user.Id, davetiye.RolId, davetiye.DavetEdenUserId);

        if (!davetiye.TumTasinmazlaraErisim && davetiye.TasinmazIds != null)
        {
            var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(davetiye.TasinmazIds) ?? [];
            foreach (var tasinmazId in ids)
            {
                _db.KullaniciYetkiKapsamlari.Add(new KullaniciYetkiKapsami
                {
                    UserId = user.Id,
                    KapsamTipi = KapsamTipi.Tasinmaz,
                    KapsamId = tasinmazId,
                });
            }
        }

        if (davetiye.BirimIds != null)
        {
            var birimIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(davetiye.BirimIds) ?? [];
            foreach (var birimId in birimIds)
            {
                _db.KullaniciYetkiKapsamlari.Add(new KullaniciYetkiKapsami
                {
                    UserId = user.Id,
                    KapsamTipi = KapsamTipi.Birim,
                    KapsamId = birimId,
                });
            }
        }

        if ((!davetiye.TumTasinmazlaraErisim && davetiye.TasinmazIds != null) || davetiye.BirimIds != null)
            await _db.SaveChangesAsync(ct);

        davetiye.Durum = DavetiyeDurum.KabulEdildi;
        davetiye.KabulTarihi = DateTime.UtcNow;
        davetiye.OlusanUserId = user.Id;
        await _db.SaveChangesAsync(ct);

        await _auditService.LogAsync("Invite.Accepted", "Davetiye", davetiye.Id.ToString(), user.Id);
        return user;
    }

    public async Task IptalEtAsync(int davetiyeId, CancellationToken ct = default)
    {
        var davetiye = await _db.Davetiyeler.FindAsync([davetiyeId], ct)
            ?? throw new InvalidOperationException("Davetiye bulunamadı.");

        if (davetiye.Durum != DavetiyeDurum.Beklemede)
            throw new InvalidOperationException("Yalnızca beklemedeki davetler iptal edilebilir.");

        davetiye.Durum = DavetiyeDurum.IptalEdildi;
        await _db.SaveChangesAsync(ct);
        await _auditService.LogAsync("Invite.Cancelled", "Davetiye", davetiyeId.ToString());
    }

    private static readonly TimeSpan YenidenGonderCooldown = TimeSpan.FromHours(1);

    public async Task YenidenGonderAsync(int davetiyeId, string davetEdenUserId, CancellationToken ct = default)
    {
        var davetiye = await _db.Davetiyeler.FindAsync([davetiyeId], ct)
            ?? throw new InvalidOperationException("Davetiye bulunamadı.");

        if (davetiye.Durum == DavetiyeDurum.KabulEdildi)
            throw new InvalidOperationException("Kabul edilmiş davetler yeniden gönderilemez.");

        if (davetiye.Durum == DavetiyeDurum.IptalEdildi)
            throw new InvalidOperationException("İptal edilmiş davetler yeniden gönderilemez.");

        var sonGonderim = davetiye.UpdatedAt ?? davetiye.CreatedAt;
        var kalanDakika = (int)(YenidenGonderCooldown - (DateTime.UtcNow - sonGonderim)).TotalMinutes;
        if (kalanDakika > 0)
            throw new InvalidOperationException($"Bu davet en son {sonGonderim.ToLocalTime():HH:mm} itibarıyla gönderildi. Yeniden göndermek için {kalanDakika} dakika beklemeniz gerekiyor.");

        var tokenResult = _tokenService.Generate(davetiye.Id.ToString(), Purpose, Ttl);
        davetiye.TokenHash = tokenResult.TokenHash;
        davetiye.ExpiresAt = tokenResult.ExpiresAt;
        davetiye.Durum = DavetiyeDurum.Beklemede;
        davetiye.DavetEdenUserId = davetEdenUserId;
        await _db.SaveChangesAsync(ct);

        await MailGonderAsync(davetiye, tokenResult.RawToken, ct);
        await _auditService.LogAsync("Invite.Resent", "Davetiye", davetiyeId.ToString(), davetiye.Email);
    }

    public async Task<List<Davetiye>> GetBekleyenlerAsync(CancellationToken ct = default)
        => await _db.Davetiyeler
            .Where(d => d.Durum == DavetiyeDurum.Beklemede)
            .Include(d => d.Rol)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public async Task SuresiDolanlariIsaretle(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.Davetiyeler
            .Where(d => d.Durum == DavetiyeDurum.Beklemede && d.ExpiresAt < now)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Durum, DavetiyeDurum.SuresiDolmus), ct);
    }

    private async Task MailGonderAsync(Davetiye davetiye, string rawToken, CancellationToken ct)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is not null
            ? $"{request.Scheme}://{request.Host}"
            : "http://localhost:5031";

        var link = $"{baseUrl}/Account/Davet?token={Uri.EscapeDataString(rawToken)}";

        var model = new DavetiyeMailModel
        {
            AdSoyad = davetiye.AdSoyad ?? davetiye.Email,
            DavetLink = link,
            SonTarih = davetiye.ExpiresAt.ToLocalTime()
        };

        string html;
        try
        {
            html = await _renderer.RenderAsync("/Views/Shared/EmailTemplates/Davetiye.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Davet mail template render hatası");
            throw;
        }

        await _mailService.SendAsync(davetiye.Email, davetiye.AdSoyad ?? davetiye.Email, "KiraTakip — Hesap Davetiyeniz", html, ct);
    }
}

public class DavetiyeMailModel
{
    public string AdSoyad { get; set; } = string.Empty;
    public string DavetLink { get; set; } = string.Empty;
    public DateTime SonTarih { get; set; }
}

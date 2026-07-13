using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class InvitationService : IInvitationService, ITransactionalService
{
    private readonly ApplicationDbContext _db;
    private readonly ISecureTokenService _tokenService;
    private readonly IMailService _mailService;
    private readonly IRazorViewToStringRenderer _renderer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRoleService _userRolService;
    private readonly ILogger<InvitationService> _logger;

    private const string Purpose = "invite";
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    public InvitationService(
        ApplicationDbContext db,
        ISecureTokenService tokenService,
        IMailService mailService,
        IRazorViewToStringRenderer renderer,
        IHttpContextAccessor httpContextAccessor,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager,
        IUserRoleService userRoleService,
        ILogger<InvitationService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _mailService = mailService;
        _renderer = renderer;
        _httpContextAccessor = httpContextAccessor;
        _auditService = auditService;
        _userManager = userManager;
        _userRolService = userRoleService;
        _logger = logger;
    }

    public async Task<Invitation> GonderAsync(string email, string? adSoyad, int rolId, string davetEdenUserId, int? tenantId = null, bool tumTasinmazlaraErisim = false, List<int>? tasinmazIds = null, List<int>? birimIds = null, CancellationToken ct = default)
    {
        var userType = tenantId.HasValue ? UserType.Tenant : UserType.Internal;
        var invitation = new Invitation
        {
            Email = email.Trim().ToLowerInvariant(),
            FullName = adSoyad,
            RoleId = rolId,
            InvitedByUserId = davetEdenUserId,
            UserType = userType,
            TenantId = tenantId,
            HasAccessToAllProperties = tumTasinmazlaraErisim,
            PropertyIds = (tasinmazIds != null && tasinmazIds.Any())
                ? System.Text.Json.JsonSerializer.Serialize(tasinmazIds)
                : null,
            UnitIds = (birimIds != null && birimIds.Any())
                ? System.Text.Json.JsonSerializer.Serialize(birimIds)
                : null,
            ExpiresAt = DateTime.UtcNow.Add(Ttl),
            Status = InvitationStatus.Pending,
        };

        _db.Davetiyeler.Add(invitation);
        await _db.SaveChangesAsync(ct);

        var tokenResult = _tokenService.Generate(invitation.Id.ToString(), Purpose, Ttl);
        invitation.TokenHash = tokenResult.TokenHash;
        invitation.ExpiresAt = tokenResult.ExpiresAt;
        await _db.SaveChangesAsync(ct);

        await MailGonderAsync(invitation, tokenResult.RawToken, ct);

        await _auditService.LogAsync("Invite.Sent", "Invitation", invitation.Id.ToString(), email);
        return invitation;
    }

    public async Task<(bool Success, string? Error, Invitation? Invitation)> DogrulaAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = _tokenService.ComputeHash(rawToken);
        var invitation = await _db.Davetiyeler
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.TokenHash == hash, ct);

        if (invitation is null)
            return (false, "Davet linki geçersiz.", null);

        if (invitation.Status == InvitationStatus.Cancelled)
            return (false, "Bu davet iptal edilmiş.", null);

        if (invitation.Status == InvitationStatus.Accepted)
            return (false, "Bu davet daha önce kullanılmış.", null);

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _db.SaveChangesAsync(ct);
            return (false, "Davet linkinin süresi dolmuş. Yeni davet talep edin.", null);
        }

        if (!_tokenService.TryValidate(rawToken, invitation.Id.ToString(), Purpose, out var reason))
            return (false, reason ?? "Token doğrulanamadı.", null);

        return (true, null, invitation);
    }

    public async Task<ApplicationUser> KabulEtAsync(Invitation invitation, string adSoyad, string password, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = invitation.Email,
            Email = invitation.Email,
            AdSoyad = adSoyad,
            EmailConfirmed = true,
            UserType = invitation.UserType,
            TenantId = invitation.TenantId,
            TumTasinmazlaraErisim = invitation.HasAccessToAllProperties,
            IsActive = true,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userRolService.AddRoleByRolIdAsync(user.Id, invitation.RoleId, invitation.InvitedByUserId);

        if (!invitation.HasAccessToAllProperties && invitation.PropertyIds != null)
        {
            var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(invitation.PropertyIds) ?? [];
            foreach (var propertyId in ids)
            {
                _db.KullaniciYetkiKapsamlari.Add(new UserPermissionScope
                {
                    UserId = user.Id,
                    ScopeType = ScopeType.Property,
                    ScopeId = propertyId,
                });
            }
        }

        if (invitation.UnitIds != null)
        {
            var birimIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(invitation.UnitIds) ?? [];
            foreach (var unitId in birimIds)
            {
                _db.KullaniciYetkiKapsamlari.Add(new UserPermissionScope
                {
                    UserId = user.Id,
                    ScopeType = ScopeType.Unit,
                    ScopeId = unitId,
                });
            }
        }

        if ((!invitation.HasAccessToAllProperties && invitation.PropertyIds != null) || invitation.UnitIds != null)
            await _db.SaveChangesAsync(ct);

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.CreatedUserId = user.Id;
        await _db.SaveChangesAsync(ct);

        await _auditService.LogAsync("Invite.Accepted", "Invitation", invitation.Id.ToString(), user.Id);
        return user;
    }

    public async Task IptalEtAsync(int invitationId, CancellationToken ct = default)
    {
        var invitation = await _db.Davetiyeler.FindAsync([invitationId], ct)
            ?? throw new InvalidOperationException("Invitation bulunamadı.");

        if (invitation.Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Yalnızca beklemedeki davetler iptal edilebilir.");

        invitation.Status = InvitationStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        await _auditService.LogAsync("Invite.Cancelled", "Invitation", invitationId.ToString());
    }

    private static readonly TimeSpan YenidenGonderCooldown = TimeSpan.FromHours(1);

    public async Task YenidenGonderAsync(int invitationId, string davetEdenUserId, CancellationToken ct = default)
    {
        var invitation = await _db.Davetiyeler.FindAsync([invitationId], ct)
            ?? throw new InvalidOperationException("Invitation bulunamadı.");

        if (invitation.Status == InvitationStatus.Accepted)
            throw new InvalidOperationException("Kabul edilmiş davetler yeniden gönderilemez.");

        if (invitation.Status == InvitationStatus.Cancelled)
            throw new InvalidOperationException("İptal edilmiş davetler yeniden gönderilemez.");

        var sonGonderim = invitation.UpdatedAt ?? invitation.CreatedAt;
        var kalanDakika = (int)(YenidenGonderCooldown - (DateTime.UtcNow - sonGonderim)).TotalMinutes;
        if (kalanDakika > 0)
            throw new InvalidOperationException($"Bu davet en son {sonGonderim.ToLocalTime():HH:mm} itibarıyla gönderildi. Yeniden göndermek için {kalanDakika} dakika beklemeniz gerekiyor.");

        var tokenResult = _tokenService.Generate(invitation.Id.ToString(), Purpose, Ttl);
        invitation.TokenHash = tokenResult.TokenHash;
        invitation.ExpiresAt = tokenResult.ExpiresAt;
        invitation.Status = InvitationStatus.Pending;
        invitation.InvitedByUserId = davetEdenUserId;
        await _db.SaveChangesAsync(ct);

        await MailGonderAsync(invitation, tokenResult.RawToken, ct);
        await _auditService.LogAsync("Invite.Resent", "Invitation", invitationId.ToString(), invitation.Email);
    }

    public async Task<List<Invitation>> GetBekleyenlerAsync(CancellationToken ct = default)
        => await _db.Davetiyeler
            .Where(d => d.Status == InvitationStatus.Pending && d.TenantId == null)
            .Include(d => d.Role)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public async Task SuresiDolanlariIsaretle(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.Davetiyeler
            .Where(d => d.Status == InvitationStatus.Pending && d.ExpiresAt < now)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Status, InvitationStatus.Expired), ct);
    }

    private async Task MailGonderAsync(Invitation invitation, string rawToken, CancellationToken ct)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is not null
            ? $"{request.Scheme}://{request.Host}"
            : "http://localhost:5031";

        var link = $"{baseUrl}/Account/Davet?token={Uri.EscapeDataString(rawToken)}";

        var model = new InvitationMailModel
        {
            AdSoyad = invitation.FullName ?? invitation.Email,
            DavetLink = link,
            SonTarih = invitation.ExpiresAt.ToLocalTime()
        };

        string html;
        try
        {
            html = await _renderer.RenderAsync("/Views/Shared/EmailTemplates/Invitation.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Davet mail template render hatası");
            throw;
        }

        await _mailService.SendAsync(invitation.Email, invitation.FullName ?? invitation.Email, "KiraTakip — Hesap Davetiyeniz", html, ct);
    }
}

public class InvitationMailModel
{
    public string AdSoyad { get; set; } = string.Empty;
    public string DavetLink { get; set; } = string.Empty;
    public DateTime SonTarih { get; set; }
}

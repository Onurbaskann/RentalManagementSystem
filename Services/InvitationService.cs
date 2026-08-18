using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos.Invitation;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Services;

public class InvitationService(
    IInvitationRepository invitationRepository,
    IUserPermissionScopeRepository scopeRepository,
    IUnitOfWork unitOfWork,
    ISecureTokenService tokenService,
    IMailService mailService,
    IRazorViewToStringRenderer renderer,
    IHttpContextAccessor httpContextAccessor,
    IAuditService auditService,
    UserManager<ApplicationUser> userManager,
    IUserRoleService userRoleService,
    IPermissionScopeCache permissionScopeCache,
    IOperationalPolicyProvider operationalPolicyProvider,
    ILogger<InvitationService> logger) : IInvitationService, ITransactionalService
{
    private const string Purpose = "invite";

    public async Task<Invitation> SendAsync(SendInvitationInput input, CancellationToken ct = default)
    {
        var ttl = GetTtl();
        var userType = input.TenantId.HasValue ? UserType.Tenant : UserType.Internal;
        var invitation = new Invitation
        {
            Email = input.Email.Trim().ToLowerInvariant(),
            FullName = input.FullName,
            RoleId = input.RoleId,
            InvitedByUserId = input.InvitedByUserId,
            UserType = userType,
            TenantId = input.TenantId,
            HasAccessToAllProperties = input.HasAccessToAllProperties,
            PropertyIds = (input.PropertyIds != null && input.PropertyIds.Any())
                ? System.Text.Json.JsonSerializer.Serialize(input.PropertyIds)
                : null,
            UnitIds = (input.UnitIds != null && input.UnitIds.Any())
                ? System.Text.Json.JsonSerializer.Serialize(input.UnitIds)
                : null,
            ExpiresAt = DateTime.UtcNow.Add(ttl),
            Status = InvitationStatus.Pending,
        };

        await invitationRepository.AddAsync(invitation);
        await unitOfWork.SaveChangesAsync(ct);

        var tokenResult = tokenService.Generate(invitation.Id.ToString(), Purpose, ttl);
        invitation.TokenHash = tokenResult.TokenHash;
        invitation.ExpiresAt = tokenResult.ExpiresAt;
        await unitOfWork.SaveChangesAsync(ct);

        await SendEmailAsync(invitation, tokenResult.RawToken, ct);

        await auditService.LogAsync("Invite.Sent", "Invitation", invitation.Id.ToString(), input.Email);
        return invitation;
    }

    public async Task<(bool Success, string? Error, Invitation? Invitation)> ValidateAsync(string token, CancellationToken ct = default)
    {
        var hash = tokenService.ComputeHash(token);
        var invitation = await invitationRepository.GetByTokenHashIgnoringFiltersAsync(hash, ct);

        if (invitation is null)
            return (false, "Davet linki geçersiz.", null);

        if (invitation.Status == InvitationStatus.Cancelled)
            return (false, "Bu davet iptal edilmiş.", null);

        if (invitation.Status == InvitationStatus.Accepted)
            return (false, "Bu davet daha önce kullanılmış.", null);

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;

            await unitOfWork.SaveChangesAsync(ct);

            return (false, "Davet linkinin süresi dolmuş. Yeni davet talep edin.", null);
        }

        if (!tokenService.TryValidate(token, invitation.Id.ToString(), Purpose, out var reason))
            return (false, reason ?? "Token doğrulanamadı.", null);

        return (true, null, invitation);
    }

    public async Task<ApplicationUser> AcceptAsync(Invitation invitation, AcceptInput input, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = invitation.Email,
            Email = invitation.Email,
            AdSoyad = input.FullName,
            EmailConfirmed = true,
            UserType = invitation.UserType,
            TenantId = invitation.TenantId,
            TumTasinmazlaraErisim = invitation.HasAccessToAllProperties,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, input.Password);
        Guard.Against(!result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));

        await userRoleService.AddRoleByRolIdAsync(user.Id, invitation.RoleId, invitation.InvitedByUserId);

        var scopes = new List<UserPermissionScope>();
        if (!invitation.HasAccessToAllProperties && invitation.PropertyIds != null)
        {
            var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(invitation.PropertyIds) ?? [];
            foreach (var propertyId in ids)
            {
                scopes.Add(new UserPermissionScope
                {
                    UserId = user.Id,
                    ScopeType = ScopeType.Property,
                    ScopeId = propertyId,
                });
            }
        }

        if (invitation.UnitIds != null)
        {
            var unitIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(invitation.UnitIds) ?? [];
            foreach (var unitId in unitIds)
            {
                scopes.Add(new UserPermissionScope
                {
                    UserId = user.Id,
                    ScopeType = ScopeType.Unit,
                    ScopeId = unitId,
                });
            }
        }

        if (scopes.Count > 0)
        {
            await scopeRepository.AddRangeAsync(scopes, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.CreatedUserId = user.Id;
        await unitOfWork.SaveChangesAsync(ct);
        permissionScopeCache.Invalidate(user.Id);

        await auditService.LogAsync("Invite.Accepted", "Invitation", invitation.Id.ToString(), user.Id);
        return user;
    }

    public async Task CancelAsync(int invitationId, CancellationToken ct = default)
    {
        var invitation = Guard.NotFound(
            await invitationRepository.GetByIdAsync(invitationId),
            "Davetiye bulunamadı.");

        Guard.Conflict(
            invitation.Status != InvitationStatus.Pending,
            "Yalnızca beklemedeki davetler iptal edilebilir.");

        invitation.Status = InvitationStatus.Cancelled;

        await unitOfWork.SaveChangesAsync(ct);
        await auditService.LogAsync("Invite.Cancelled", "Invitation", invitationId.ToString());
    }

    public async Task ResendAsync(int invitationId, string invitedByUserId, CancellationToken ct = default)
    {
        var invitation = Guard.NotFound(
            await invitationRepository.GetByIdAsync(invitationId),
            "Davetiye bulunamadı.");

        Guard.Conflict(
            invitation.Status == InvitationStatus.Accepted,
            "Kabul edilmiş davetler yeniden gönderilemez.");

        Guard.Conflict(
            invitation.Status == InvitationStatus.Cancelled,
            "İptal edilmiş davetler yeniden gönderilemez.");

        var lastSentAt = invitation.UpdatedAt ?? invitation.CreatedAt;
        var resendCooldown = TimeSpan.FromMinutes(
            operationalPolicyProvider.Current.InvitationResendCooldownMinutes);
        var remainingMinutes = (int)(resendCooldown - (DateTime.UtcNow - lastSentAt)).TotalMinutes;
        Guard.Conflict(
            remainingMinutes > 0,
            $"Bu davet en son {lastSentAt.ToLocalTime():HH:mm} itibarıyla gönderildi. Yeniden göndermek için {remainingMinutes} dakika beklemeniz gerekiyor.");

        var tokenResult = tokenService.Generate(invitation.Id.ToString(), Purpose, GetTtl());

        invitation.TokenHash = tokenResult.TokenHash;
        invitation.ExpiresAt = tokenResult.ExpiresAt;
        invitation.Status = InvitationStatus.Pending;
        invitation.InvitedByUserId = invitedByUserId;

        await unitOfWork.SaveChangesAsync(ct);

        await SendEmailAsync(invitation, tokenResult.RawToken, ct);
        await auditService.LogAsync("Invite.Resent", "Invitation", invitationId.ToString(), invitation.Email);
    }

    public async Task<List<Invitation>> GetPendingAsync(CancellationToken ct = default)
        => await invitationRepository.GetPendingInternalAsync(ct);

    public async Task MarkExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await invitationRepository.MarkExpiredAsync(now, ct);
    }

    private async Task SendEmailAsync(Invitation invitation, string rawToken, CancellationToken ct)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        var baseUrl = request is not null
            ? $"{request.Scheme}://{request.Host}"
            : "http://localhost:5031";

        var link = $"{baseUrl}/Account/Invite?token={Uri.EscapeDataString(rawToken)}";

        var model = new InvitationMailModel
        {
            FullName = invitation.FullName ?? invitation.Email,
            InvitationLink = link,
            ExpiresAt = invitation.ExpiresAt.ToLocalTime()
        };

        string html;
        try
        {
            html = await renderer.RenderAsync("/Views/Shared/EmailTemplates/Invitation.cshtml", model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Davet mail template render hatası");
            throw;
        }

        await mailService.SendAsync(invitation.Email, invitation.FullName ?? invitation.Email, "KiraTakip — Hesap Davetiyeniz", html, ct);
    }

    private TimeSpan GetTtl()
        => TimeSpan.FromDays(operationalPolicyProvider.Current.InvitationValidityDays);
}

public class InvitationMailModel
{
    public string FullName { get; set; } = string.Empty;
    public string InvitationLink { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

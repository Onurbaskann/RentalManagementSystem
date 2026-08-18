using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Invitation;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Services;

public class TenantUserService(
    IApplicationUserRepository userRepository,
    IInvitationRepository invitationRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    ILeaseRepository leaseRepository,
    IUnitRepository unitRepository,
    IUserPermissionScopeRepository permissionScopeRepository,
    UserManager<ApplicationUser> userManager,
    IInvitationService invitationService,
    IAuditService auditService,
    IUserSecurityService userSecurityService,
    IPermissionScopeCache permissionScopeCache,
    IUnitOfWork unitOfWork) : ITenantUserService, ITransactionalService
{
    public async Task EnsureTenantManagerExistsAsync(
        EnsureTenantManagerExistsInput input,
        CancellationToken ct = default)
    {
        Guard.Conflict(
            !await userRepository.HasTenantManagerAsync(
                input.TenantId,
                input.ExcludedUserId,
                input.ExcludedRoleId,
                ct),
            "Sistemde en az bir aktif Firma Yetkilisi bulunmalıdır. Bu işlem onaylanamadı.",
            "TENANT_MANAGER_REQUIRED");
    }

    public async Task<TenantUsersListDto> GetTenantUsersListAsync(GetTenantUsersListInput input)
    {
        var tenant = Guard.NotFound(
            await tenantRepository.GetActiveByIdAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_USER_TENANT_NOT_FOUND");

        var users = await userRepository.GetTenantUserListAsync(input.TenantId);
        var invitations = await invitationRepository.GetPendingTenantListAsync(
            input.TenantId,
            DateTime.UtcNow);

        return new TenantUsersListDto(tenant.DisplayName, users, invitations);
    }

    public async Task<TenantUsersPageDto> GetTenantUsersPageAsync(GetTenantUsersPageInput input)
    {
        var tenant = Guard.NotFound(
            await tenantRepository.GetActiveByIdAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_USER_TENANT_NOT_FOUND");
        var users = await userRepository.GetTenantUserPageAsync(input.TenantId, input.Query);
        var invitations = await invitationRepository.GetPendingTenantListAsync(
            input.TenantId,
            DateTime.UtcNow);

        return new TenantUsersPageDto(tenant.DisplayName, users, invitations);
    }

    public async Task ToggleUserActiveAsync(ToggleTenantUserActiveInput input)
    {
        Guard.Forbidden(
            input.UserId == input.CurrentUserId,
            "Kendi hesabınızı pasif hale getiremezsiniz.",
            "TENANT_USER_SELF_DEACTIVATION");

        var user = Guard.NotFound(
            await userRepository.GetUserByIdAndTenantIdAsync(input.UserId, input.TenantId),
            "Kullanıcı bulunamadı.",
            "TENANT_USER_NOT_FOUND");

        if (user.IsActive)
        {
            await EnsureTenantManagerExistsAsync(
                new EnsureTenantManagerExistsInput(input.TenantId, ExcludedUserId: user.Id));
        }

        user.IsActive = !user.IsActive;
        var updateResult = await userManager.UpdateAsync(user);
        Guard.Against(
            !updateResult.Succeeded,
            "Kullanıcı durumu güncellenemedi.",
            "TENANT_USER_STATUS_UPDATE_FAILED");

        await userSecurityService.UpdateStampAsync(user.Id);
        permissionScopeCache.Invalidate(user.Id);
        await auditService.LogAsync(
            user.IsActive ? "User.Activated" : "User.Deactivated",
            "ApplicationUser",
            user.Id,
            user.Email);
    }

    public async Task CancelInvitationAsync(CancelTenantInvitationInput input)
    {
        Guard.NotFound(
            await invitationRepository.GetByIdAndTenantIdAsync(input.InvitationId, input.TenantId),
            "Davetiye bulunamadı.",
            "TENANT_INVITATION_NOT_FOUND");

        await invitationService.CancelAsync(input.InvitationId);
    }

    public async Task ResendInvitationAsync(ResendTenantInvitationInput input)
    {
        Guard.NotFound(
            await invitationRepository.GetByIdAndTenantIdAsync(input.InvitationId, input.TenantId),
            "Davetiye bulunamadı.",
            "TENANT_INVITATION_NOT_FOUND");

        await invitationService.ResendAsync(input.InvitationId, input.ResentByUserId);
    }

    public async Task<TenantInviteDataDto> GetInviteDataAsync(GetInviteDataInput input)
    {
        var tenant = Guard.NotFound(
            await tenantRepository.GetActiveByIdAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_USER_TENANT_NOT_FOUND");
        var roles = await roleRepository.GetActiveTenantRolesAsync(input.TenantId);
        var units = await leaseRepository.GetActiveLeaseUnitsByTenantIdAsync(input.TenantId);

        return new TenantInviteDataDto(
            tenant.DisplayName,
            roles.Select(role => new RoleLookupDto(role.Id, role.Name)).ToList(),
            units);
    }

    public async Task SendInvitationAsync(SendTenantInvitationInput input)
    {
        Guard.NotFound(
            await tenantRepository.GetActiveByIdAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_USER_TENANT_NOT_FOUND");

        var normalizedEmail = userManager.NormalizeEmail(input.Email.Trim());
        Guard.InvalidField(
            await userRepository.EmailExistsAsync(normalizedEmail),
            nameof(input.Email),
            "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten bulunmaktadır.",
            "TENANT_INVITATION_EMAIL_IN_USE");
        Guard.InvalidField(
            await invitationRepository.HasPendingForTenantEmailAsync(
                input.TenantId,
                input.Email.Trim(),
                DateTime.UtcNow),
            nameof(input.Email),
            "Bu e-posta adresi için bekleyen bir davet zaten bulunmaktadır.",
            "TENANT_INVITATION_ALREADY_PENDING");

        var role = await roleRepository.GetTenantRoleByIdAsync(input.RoleId, input.TenantId);
        Guard.InvalidField(
            role == null,
            nameof(input.RoleId),
            "Geçersiz rol seçildi.",
            "TENANT_INVITATION_INVALID_ROLE");

        var unitIds = input.UnitIds?.Distinct().ToList();
        if (unitIds is { Count: > 0 })
        {
            var validUnits = await leaseRepository.GetActiveLeaseUnitsByTenantIdAsync(input.TenantId);
            var validUnitIds = validUnits.Select(unit => unit.Id).ToHashSet();
            Guard.InvalidField(
                unitIds.Any(unitId => !validUnitIds.Contains(unitId)),
                nameof(input.UnitIds),
                "Geçersiz birim seçildi.",
                "TENANT_INVITATION_INVALID_UNIT");
        }

        await invitationService.SendAsync(new SendInvitationInput(
            input.Email.Trim(),
            input.FullName?.Trim(),
            input.RoleId,
            input.InvitedByUserId,
            input.TenantId,
            HasAccessToAllProperties: unitIds is not { Count: > 0 },
            UnitIds: unitIds));
    }

    public async Task<InitialTenantInvitationResultDto> TrySendInitialRepresentativeInvitationAsync(
        SendInitialTenantRepresentativeInput input)
    {
        try
        {
            var tenantManagerRole = await roleRepository.GetAsync(role =>
                role.TenantId == null
                && role.Name == RoleNames.KiraciYoneticisi
                && role.IsActive);
            if (tenantManagerRole == null)
            {
                return new InitialTenantInvitationResultDto(false,
                    "Firma Yetkilisi rolü tanımlı değil; davet gönderilemedi. Kiracı > Kullanıcılar ekranından tekrar deneyebilirsiniz.");
            }

            await invitationService.SendAsync(new SendInvitationInput(
                input.Email,
                input.FullName,
                tenantManagerRole.Id,
                input.InvitedByUserId,
                input.TenantId,
                HasAccessToAllProperties: true));

            return new InitialTenantInvitationResultDto(true, null);
        }
        catch
        {
            return new InitialTenantInvitationResultDto(false,
                "İlk yetkili daveti gönderilemedi; Kiracı > Kullanıcılar ekranından tekrar deneyebilirsiniz.");
        }
    }

    public async Task<TenantUserEditDataDto> GetTenantUserForEditAsync(
        GetTenantUserForEditInput input)
    {
        Guard.Forbidden(
            input.UserId == input.CurrentUserId,
            "Kendi hesabınızı bu ekrandan değiştiremezsiniz.",
            "TENANT_USER_SELF_EDIT");

        var user = Guard.NotFound(
            await userRepository.GetTenantUserForEditAsync(input.UserId, input.TenantId),
            "Kullanıcı bulunamadı.",
            "TENANT_USER_NOT_FOUND");
        var roles = await GetEditRoleOptionsAsync(input.TenantId, user.RoleId);
        var (leaseUnits, reservableUnits) = await GetAssignableUnitsAsync(
            input.TenantId,
            input.AccessScope);
        var selectedUnitIds = await permissionScopeRepository.GetScopeIdsAsync(
            user.Id,
            ScopeType.Unit);

        return new TenantUserEditDataDto(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            user.RoleId,
            user.HasAccessToAllUnits,
            selectedUnitIds,
            roles,
            leaseUnits,
            reservableUnits);
    }

    public async Task EditTenantUserAsync(EditTenantUserInput input)
    {
        Guard.Forbidden(
            input.UserId == input.CurrentUserId,
            "Kendi hesabınızı bu ekrandan değiştiremezsiniz.",
            "TENANT_USER_SELF_EDIT");

        var user = Guard.NotFound(
            await userRepository.GetUserByIdAndTenantIdAsync(input.UserId, input.TenantId),
            "Kullanıcı bulunamadı.",
            "TENANT_USER_NOT_FOUND");
        var currentRole = await userRoleRepository.GetUserRoleInfoAsync(user.Id);
        var newRole = await roleRepository.GetTenantRoleByIdAsync(input.RoleId, input.TenantId);
        Guard.InvalidField(
            newRole == null || newRole.IsSystemRole && currentRole?.RoleId != newRole.Id,
            nameof(input.RoleId),
            "Geçersiz rol seçildi.",
            "TENANT_USER_INVALID_ROLE");

        var selectedUnitIds = input.HasAccessToAllUnits
            ? []
            : input.UnitIds.Distinct().ToList();
        if (!input.HasAccessToAllUnits)
        {
            var (leaseUnits, reservableUnits) = await GetAssignableUnitsAsync(
                input.TenantId,
                input.AccessScope);
            var assignableUnitIds = leaseUnits.Select(unit => unit.Id)
                .Concat(reservableUnits.Select(unit => unit.Id))
                .ToHashSet();
            Guard.InvalidField(
                selectedUnitIds.Count == 0
                    || selectedUnitIds.Any(unitId => !assignableUnitIds.Contains(unitId)),
                nameof(input.UnitIds),
                "Seçilen birimlerden en az biri bu kullanıcıya atanamaz.",
                "TENANT_USER_INVALID_UNIT_SCOPE");
        }

        if (currentRole?.RoleName == RoleNames.KiraciYoneticisi
            && newRole!.Name != RoleNames.KiraciYoneticisi)
        {
            await EnsureTenantManagerExistsAsync(
                new EnsureTenantManagerExistsInput(input.TenantId, ExcludedUserId: user.Id));
        }

        var existingRoles = await userRoleRepository.GetAllByUserIgnoringFiltersAsync(user.Id);
        userRoleRepository.RemoveRange(existingRoles);
        await userRoleRepository.AddAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = input.RoleId,
            CreatedBy = input.CurrentUserId
        });

        user.AdSoyad = input.FullName.Trim();
        var previousGlobalAccess = user.TumTasinmazlaraErisim;
        var previousUnitIds = await permissionScopeRepository.GetScopeIdsAsync(
            user.Id,
            ScopeType.Unit);
        user.TumTasinmazlaraErisim = input.HasAccessToAllUnits;
        var updateResult = await userManager.UpdateAsync(user);
        Guard.InvalidField(
            !updateResult.Succeeded,
            nameof(input.FullName),
            "Kullanıcı güncellenemedi.",
            "TENANT_USER_UPDATE_FAILED");
        await permissionScopeRepository.ReplaceAsync(user.Id, [], selectedUnitIds);
        await unitOfWork.SaveChangesAsync();

        await userSecurityService.UpdateStampAsync(user.Id);
        permissionScopeCache.Invalidate(user.Id);
        await auditService.LogAsync(
            "User.RoleChanged",
            "ApplicationUser",
            user.Id,
            $"KiraciId:{input.TenantId}");

        if (previousGlobalAccess != input.HasAccessToAllUnits
            || !previousUnitIds.Order().SequenceEqual(selectedUnitIds.Order()))
        {
            await auditService.LogAsync(
                "User.ScopeChanged",
                "ApplicationUser",
                user.Id,
                $"KiraciId:{input.TenantId};TumBirimler:{input.HasAccessToAllUnits};BirimSayisi:{selectedUnitIds.Count}");
        }
    }

    private async Task<List<RoleLookupDto>> GetEditRoleOptionsAsync(int tenantId, int currentRoleId)
    {
        var roles = await roleRepository.GetActiveTenantRolesAsync(tenantId);
        return roles
            .Where(role => !role.IsSystemRole || role.Id == currentRoleId)
            .Select(role => new RoleLookupDto(role.Id, role.Name))
            .ToList();
    }

    private async Task<(List<UnitLookupDto> LeaseUnits, List<UnitListItemDto> ReservableUnits)>
        GetAssignableUnitsAsync(int tenantId, ReservationAccessScopeInput accessScope)
    {
        var propertyIds = accessScope.PropertyIds?.ToList();
        var unitIds = accessScope.UnitIds?.ToList();
        var leaseUnits = await leaseRepository.GetActiveLeaseUnitsByTenantIdAsync(
            tenantId,
            propertyIds,
            unitIds);
        var reservableUnits = await unitRepository.GetReservableUnitsAsync(
            propertyIds,
            unitIds);

        var leaseUnitIds = leaseUnits.Select(unit => unit.Id).ToHashSet();
        reservableUnits = reservableUnits
            .Where(unit => !leaseUnitIds.Contains(unit.Id))
            .ToList();
        return (leaseUnits, reservableUnits);
    }
}

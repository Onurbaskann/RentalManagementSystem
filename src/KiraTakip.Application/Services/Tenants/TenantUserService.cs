using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Auditing;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Invitation;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Tenants;
using KiraTakip.Models.Dtos.TenantUser;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Services.Tenants;

public class TenantUserService(
    IApplicationUserRepository userRepository,
    IInvitationRepository invitationRepository,
    ITenantRepository tenantRepository,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    ILeaseRepository leaseRepository,
    IReservationRepository reservationRepository,
    IUserPermissionScopeRepository permissionScopeRepository,
    IApplicationUserManager userManager,
    IInvitationService invitationService,
    IAuditService auditService,
    IUserSecurityService userSecurityService,
    IPermissionScopeCache permissionScopeCache,
    IUserPermissionCacheInvalidator permissionCacheInvalidator,
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
            user.IsActive ? AuditEventTypes.UserActivated : AuditEventTypes.UserDeactivated,
            AuditEntityTypes.ApplicationUser,
            user.Id,
            AuditDetails.Serialize(new { email = user.Email }));
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
        var canAssignSystemRole = await CanAssignSystemRoleAsync(input.ActorUserId, RoleAssignmentOperation.Invitation);
        var visibleRoles = roles
            .Where(role => canAssignSystemRole
                || !role.IsSystemRole && role.Name != RoleNames.KiraciYoneticisi)
            .Select(role => new RoleLookupDto(role.Id, role.Name))
            .ToList();
        var spaces = await leaseRepository.GetActiveLeaseSpacesByTenantIdAsync(input.TenantId);

        return new TenantInviteDataDto(
            tenant.DisplayName,
            visibleRoles,
            spaces);
    }

    public async Task SendInvitationAsync(SendTenantInvitationInput input)
    {
        var tenant = Guard.NotFound(
            await tenantRepository.GetActiveByIdAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_USER_TENANT_NOT_FOUND");

        var role = await roleRepository.GetTenantRoleByIdAsync(input.RoleId, input.TenantId);
        Guard.InvalidField(
            role == null,
            nameof(input.RoleId),
            "Geçersiz rol seçildi.",
            "TENANT_INVITATION_INVALID_ROLE");

        if (role.IsSystemRole || role.Name == RoleNames.KiraciYoneticisi)
        {
            var canAssign = await CanAssignSystemRoleAsync(input.InvitedByUserId, RoleAssignmentOperation.Invitation);
            Guard.Forbidden(
                !canAssign,
                "Kiracı Yöneticisi rolü için davet gönderme yetkiniz bulunmamaktadır.",
                "TENANT_INVITATION_FORBIDDEN_ROLE");
        }

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

        var spaceIds = input.SpaceIds?.Distinct().ToList();
        if (spaceIds is { Count: > 0 })
        {
            var validSpaces = await leaseRepository.GetActiveLeaseSpacesByTenantIdAsync(input.TenantId);
            var validSpaceIds = validSpaces.Select(space => space.Id).ToHashSet();
            Guard.InvalidField(
                spaceIds.Any(spaceId => !validSpaceIds.Contains(spaceId)),
                nameof(input.SpaceIds),
                "Geçersiz alan seçildi.",
                "TENANT_INVITATION_INVALID_SPACE");
        }

        await invitationService.SendAsync(new SendInvitationInput(
            input.Email.Trim(),
            input.FullName?.Trim(),
            input.RoleId,
            input.InvitedByUserId,
            input.TenantId,
            HasAccessToAllSpaces: spaceIds is not { Count: > 0 },
            SpaceIds: spaceIds));
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
                HasAccessToAllSpaces: true));

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
        var roles = await GetEditRoleOptionsAsync(input.TenantId, user.RoleId, input.CurrentUserId);
        var (leaseSpaces, reservableSpaces) = await GetAssignableSpacesAsync(
            input.TenantId,
            input.AccessScope);
        var selectedSpaceIds = await permissionScopeRepository.GetScopeIdsAsync(
            user.Id,
            ScopeType.Space);

        return new TenantUserEditDataDto(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            user.RoleId,
            user.HasAccessToAllSpaces,
            selectedSpaceIds,
            roles,
            leaseSpaces,
            reservableSpaces);
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
            newRole == null,
            nameof(input.RoleId),
            "Geçersiz rol seçildi.",
            "TENANT_USER_INVALID_ROLE");

        var roleChanged = currentRole?.RoleId != input.RoleId;

        var changesSystemRole = roleChanged
            && (newRole!.IsSystemRole
                || newRole.Name == RoleNames.KiraciYoneticisi
                || currentRole?.RoleName == RoleNames.KiraciYoneticisi);

        if (changesSystemRole)
        {
            var canAssign = await CanAssignSystemRoleAsync(input.CurrentUserId, RoleAssignmentOperation.UserEdit);
            Guard.Forbidden(
                !canAssign,
                "Kiracı Yöneticisi rolünü değiştirmek için gerekli işlem yetkiniz bulunmamaktadır.",
                "TENANT_USER_ROLE_ASSIGNMENT_FORBIDDEN");
        }

        var selectedSpaceIds = input.HasAccessToAllSpaces
            ? []
            : input.SpaceIds.Distinct().ToList();
        if (!input.HasAccessToAllSpaces)
        {
            var (leaseSpaces, reservableSpaces) = await GetAssignableSpacesAsync(
                input.TenantId,
                input.AccessScope);
            var assignableSpaceIds = leaseSpaces.Select(space => space.Id)
                .Concat(reservableSpaces.Select(space => space.Id))
                .ToHashSet();
            Guard.InvalidField(
                selectedSpaceIds.Count == 0
                    || selectedSpaceIds.Any(spaceId => !assignableSpaceIds.Contains(spaceId)),
                nameof(input.SpaceIds),
                "Seçilen alanlardan en az biri bu kullanıcıya atanamaz.",
                "TENANT_USER_INVALID_SPACE_SCOPE");
        }

        if (roleChanged
            && currentRole?.RoleName == RoleNames.KiraciYoneticisi
            && newRole!.Name != RoleNames.KiraciYoneticisi)
        {
            await EnsureTenantManagerExistsAsync(
                new EnsureTenantManagerExistsInput(input.TenantId, ExcludedUserId: user.Id));
        }

        if (roleChanged)
        {
            var existingRoles = await userRoleRepository.GetAllByUserIgnoringFiltersAsync(user.Id);
            userRoleRepository.RemoveRange(existingRoles);
            await userRoleRepository.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = input.RoleId,
                CreatedBy = input.CurrentUserId
            });
        }

        user.AdSoyad = input.FullName.Trim();
        var previousGlobalAccess = user.HasAccessToAllSpaces;
        var previousSpaceIds = await permissionScopeRepository.GetScopeIdsAsync(
            user.Id,
            ScopeType.Space);
        user.HasAccessToAllSpaces = input.HasAccessToAllSpaces;
        var updateResult = await userManager.UpdateAsync(user);
        Guard.InvalidField(
            !updateResult.Succeeded,
            nameof(input.FullName),
            "Kullanıcı güncellenemedi.",
            "TENANT_USER_UPDATE_FAILED");
        await permissionScopeRepository.ReplaceAsync(user.Id, selectedSpaceIds);
        await unitOfWork.SaveChangesAsync();

        if (roleChanged)
        {
            permissionCacheInvalidator.InvalidateAfterCommit(user.Id);
            await auditService.LogAsync(
                AuditEventTypes.UserRoleChanged,
                AuditEntityTypes.ApplicationUser,
                user.Id,
                AuditDetails.Serialize(new { tenantId = input.TenantId }));
        }

        await userSecurityService.UpdateStampAsync(user.Id);
        permissionScopeCache.Invalidate(user.Id);

        if (previousGlobalAccess != input.HasAccessToAllSpaces
            || !previousSpaceIds.Order().SequenceEqual(selectedSpaceIds.Order()))
        {
            await auditService.LogAsync(
                AuditEventTypes.UserScopeChanged,
                AuditEntityTypes.ApplicationUser,
                user.Id,
                AuditDetails.Serialize(new
                {
                    tenantId = input.TenantId,
                    hasAccessToAllSpaces = input.HasAccessToAllSpaces,
                    spaceCount = selectedSpaceIds.Count
                }));
        }
    }

    public async Task<List<RoleLookupDto>> GetEditRoleOptionsAsync(
        int tenantId,
        int currentRoleId,
        string? actorUserId = null)
    {
        var roles = await roleRepository.GetActiveTenantRolesAsync(tenantId);
        var canAssignSystemRole = await CanAssignSystemRoleAsync(actorUserId, RoleAssignmentOperation.UserEdit);
        return roles
            .Where(role => canAssignSystemRole
                || !role.IsSystemRole && role.Name != RoleNames.KiraciYoneticisi)
            .Select(role => new RoleLookupDto(role.Id, role.Name))
            .ToList();
    }

    private async Task<bool> CanAssignSystemRoleAsync(string? actorUserId, RoleAssignmentOperation operation)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
            return false;

        var actor = await userRepository.GetByIdAsync(actorUserId);
        if (actor == null)
            return false;

        if (actor.UserType != UserType.Internal)
            return false;

        if (actor.IsSuperAdmin)
            return true;

        var permissions = await userRoleRepository.GetPermissionsAsync(actor.Id);
        var (modulePermission, actionPermission) = operation switch
        {
            RoleAssignmentOperation.Invitation =>
                (PermissionCatalog.Invitation.Module, PermissionCatalog.Invitation.Create),
            _ => (PermissionCatalog.User.Module, PermissionCatalog.User.Edit)
        };

        return permissions.Contains(modulePermission) && permissions.Contains(actionPermission);
    }

    private async Task<(List<LeaseSpaceOptionDto> LeaseSpaces, List<ReservationSpaceOptionDto> ReservableSpaces)>
        GetAssignableSpacesAsync(int tenantId, SpaceAccessScope accessScope)
    {
        var leaseSpaces = await leaseRepository.GetActiveLeaseSpacesByTenantIdAsync(tenantId, accessScope);
        var reservableSpaces = await reservationRepository.GetReservableSpacesAsync(accessScope);

        var leaseSpaceIds = leaseSpaces.Select(space => space.Id).ToHashSet();
        reservableSpaces = reservableSpaces
            .Where(space => !leaseSpaceIds.Contains(space.Id))
            .ToList();
        return (leaseSpaces, reservableSpaces);
    }
}

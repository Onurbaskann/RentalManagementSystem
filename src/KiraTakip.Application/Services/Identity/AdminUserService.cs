using KiraTakip.Data;
using KiraTakip.Auditing;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Invitation;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Auditing;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Spaces;
using KiraTakip.Models.Dtos.AdminUser;

namespace KiraTakip.Services.Identity;

public class AdminUserService(
    IApplicationUserRepository userRepository,
    IRoleRepository roleRepository,
    IInvitationRepository invitationRepository,
    IUserRoleRepository userRoleRepository,
    IUserPermissionScopeRepository userPermissionScopeRepository,
    IUnitOfWork unitOfWork,
    IApplicationUserManager userManager,
    ISpaceService spaceService,
    IPermissionService permissionService,
    IUserRoleService userRoleService,
    IInvitationService invitationService,
    IAuditService auditService,
    IPermissionScopeCache scopeCache) : IAdminUserService
{
    public async Task<AdminUserIndexDto> GetIndexAsync()
    {
        var internalUsers = await userRepository.GetInternalAdminUsersAsync();

        var internalItems = new List<AdminUserListItemDto>();
        foreach (var user in internalUsers)
        {
            var roles = await userRoleService.GetUserRolesAsync(user.Id);
            internalItems.Add(new AdminUserListItemDto(
                user.Id,
                user.FullName ?? user.Email ?? "—",
                user.Email ?? "—",
                roles.FirstOrDefault() ?? "—",
                user.IsActive));
        }

        var tenantUsers = await userRepository.GetAdminTenantUsersAsync();

        var tenantItems = new List<AdminTenantUserListItemDto>();
        foreach (var user in tenantUsers)
        {
            var role = await userRoleRepository.GetUserRoleInfoAsync(user.Id);

            tenantItems.Add(new AdminTenantUserListItemDto(
                user.Id,
                user.FullName ?? user.Email ?? "—",
                user.Email ?? "—",
                user.TenantId,
                user.TenantName ?? "—",
                role?.RoleName ?? "—",
                user.IsActive));
        }

        var pendingInvitations = (await invitationService.GetPendingAsync())
            .Select(invitation => new AdminPendingInvitationDto(
                invitation.Id,
                invitation.Email,
                invitation.FullName,
                invitation.ExpiresAt))
            .ToList();

        return new AdminUserIndexDto(internalItems, tenantItems, pendingInvitations);
    }

    public async Task<AdminUserIndexPageDto> GetIndexPageAsync(GetAdminUserIndexPageInput input)
    {
        var inactiveTabQuery = new TableQuery { Page = 1, Size = input.Query.SafeSize };
        var internalUsers = await userRepository.GetInternalAdminUsersPageAsync(
            input.TenantUsersTab ? inactiveTabQuery : input.Query);
        var tenantUsers = await userRepository.GetAdminTenantUsersPageAsync(
            input.TenantUsersTab ? input.Query : inactiveTabQuery);
        var pendingInvitations = (await invitationService.GetPendingAsync())
            .Select(invitation => new AdminPendingInvitationDto(
                invitation.Id,
                invitation.Email,
                invitation.FullName,
                invitation.ExpiresAt))
            .ToList();

        return new AdminUserIndexPageDto(internalUsers, tenantUsers, pendingInvitations);
    }

    public async Task<AdminUserFormOptionsDto> GetFormOptionsAsync()
    {
        var roles = await roleRepository.GetActiveInternalRoleOptionsAsync();

        var spaces = await spaceService.GetActiveOptionsAsync();

        return new AdminUserFormOptionsDto(roles, spaces);
    }

    public async Task<AdminUserEditDataDto?> GetEditDataAsync(GetAdminUserEditDataInput input)
    {
        var user = await userManager.FindByIdAsync(input.UserId);
        if (!IsManagedInternalUser(user)) return null;
        var managedUser = user!;

        var selectedSpaceIds = await userPermissionScopeRepository
            .GetScopeIdsAsync(managedUser.Id, ScopeType.Space);
        var roleId = await userRoleRepository.GetFirstRoleIdAsync(managedUser.Id) ?? 0;

        return new AdminUserEditDataDto(
            managedUser.Id,
            managedUser.AdSoyad ?? string.Empty,
            managedUser.Email ?? string.Empty,
            roleId,
            managedUser.IsActive,
            managedUser.Id == input.CurrentUserId,
            managedUser.HasAccessToAllSpaces,
            selectedSpaceIds,
            await GetFormOptionsAsync());
    }

    [Transactional]
    public async Task UpdateAsync(UpdateAdminUserInput input)
    {
        var user = await userManager.FindByIdAsync(input.UserId);
        user = Guard.NotFound(
            IsManagedInternalUser(user) ? user : null,
            "Kullanıcı bulunamadı.");

        Guard.InvalidField(
            await roleRepository.GetActiveInternalByIdAsync(input.RoleId) == null,
            nameof(input.RoleId),
            "Geçersiz rol seçildi.");

        var currentRoleId = await userRoleRepository.GetFirstRoleIdAsync(user.Id) ?? 0;
        Guard.Forbidden(
            user.Id == input.CurrentUserId && currentRoleId != input.RoleId,
            "Kendi rolünüzü değiştiremezsiniz.",
            nameof(input.RoleId));

        var spaceScopeIds = await GetValidatedScopeIdsAsync(
            input.HasAccessToAllSpaces,
            input.SpaceIds);

        await userRoleService.RemoveAllRolesAsync(user.Id);
        await userRoleService.AddRoleByRolIdAsync(user.Id, input.RoleId, input.CurrentUserId);
        await permissionService.SetUserPermissionsAsync(user.Id, Array.Empty<string>());

        user.AdSoyad = input.FullName;
        user.HasAccessToAllSpaces = input.HasAccessToAllSpaces;
        await userManager.UpdateAsync(user);

        await SetScopeAsync(user.Id, spaceScopeIds);
    }

    [Transactional]
    public async Task ToggleActiveAsync(ToggleAdminUserActiveInput input)
    {
        var user = await userManager.FindByIdAsync(input.UserId);
        user = Guard.NotFound(
            IsManagedInternalUser(user) ? user : null,
            "Kullanıcı bulunamadı.");

        Guard.Forbidden(
            user.Id == input.CurrentUserId,
            "Kendi hesabınızı pasif hale getiremezsiniz.");

        user.IsActive = !user.IsActive;

        await userManager.UpdateAsync(user);
        await userManager.UpdateSecurityStampAsync(user);

        var eventType = user.IsActive ? AuditEventTypes.UserActivated : AuditEventTypes.UserDeactivated;
        await auditService.LogAsync(eventType, AuditEntityTypes.ApplicationUser, user.Id,
            AuditDetails.Serialize(new { email = user.Email }));
    }

    public async Task SendInvitationAsync(SendAdminUserInvitationInput input)
    {
        var existingUser = await userManager.FindByEmailAsync(input.Email);
        Guard.InvalidField(
            existingUser != null,
            nameof(input.Email),
            "Bu e-posta adresi sistemde kayıtlı bir kullanıcıya ait.");

        Guard.InvalidField(
            await roleRepository.GetActiveInternalByIdAsync(input.RoleId) == null,
            nameof(input.RoleId),
            "Geçersiz rol seçildi.");

        var validatedSpaceIds = await GetValidatedScopeIdsAsync(
            input.HasAccessToAllSpaces,
            input.SpaceIds);
        var spaceIds = input.HasAccessToAllSpaces ? null : validatedSpaceIds;

        await invitationService.SendAsync(new SendInvitationInput(
            input.Email,
            input.FullName,
            input.RoleId,
            input.InvitedByUserId,
            HasAccessToAllSpaces: input.HasAccessToAllSpaces,
            SpaceIds: spaceIds));
    }

    public async Task CancelInvitationAsync(CancelAdminUserInvitationInput input)
    {
        Guard.NotFound(
            await invitationRepository.GetInternalByIdAsync(input.InvitationId),
            "Davetiye bulunamadı.");

        await invitationService.CancelAsync(input.InvitationId);
    }

    public async Task ResendInvitationAsync(ResendAdminUserInvitationInput input)
    {
        Guard.NotFound(
            await invitationRepository.GetInternalByIdAsync(input.InvitationId),
            "Davetiye bulunamadı.");

        await invitationService.ResendAsync(input.InvitationId, input.ResentByUserId);
    }

    private async Task<List<int>> GetValidatedScopeIdsAsync(
        bool hasAccessToAllSpaces,
        IReadOnlyList<int> spaceIds)
    {
        if (hasAccessToAllSpaces) return [];

        var distinctSpaceIds = spaceIds.Distinct().ToList();
        var validSpaceIds = (await spaceService.GetActiveOptionsAsync())
            .Select(space => space.Id)
            .ToHashSet();
        Guard.InvalidField(
            distinctSpaceIds.Any(spaceId => !validSpaceIds.Contains(spaceId)),
            "SelectedSpaceIds",
            "Geçersiz alan seçildi.");

        return distinctSpaceIds;
    }

    private static bool IsManagedInternalUser(ApplicationUser? user)
        => user is { IsSuperAdmin: false, UserType: UserType.Internal, TenantId: null };

    private async Task SetScopeAsync(string userId, List<int> spaceIds)
    {
        await userPermissionScopeRepository.ReplaceAsync(userId, spaceIds);
        await unitOfWork.SaveChangesAsync();

        scopeCache.Invalidate(userId);

        var details = AuditDetails.Serialize(new
        {
            spaceCount = spaceIds.Count,
            cleared = spaceIds.Count == 0
        });

        await auditService.LogAsync(AuditEventTypes.UserScopeChanged, AuditEntityTypes.ApplicationUser, userId, details);
    }
}

using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Invitation;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Services;

public class AdminUserService(
    IApplicationUserRepository userRepository,
    IRoleRepository roleRepository,
    IInvitationRepository invitationRepository,
    IUserRoleRepository userRoleRepository,
    IUnitRepository unitRepository,
    IUserPermissionScopeRepository userPermissionScopeRepository,
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    IPropertyService propertyService,
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

        var properties = (await propertyService.GetAllAsync(new GetPropertiesInput()))
            .Select(property => new AdminUserPropertyOptionDto(
                property.Id,
                property.Name,
                $"{property.City} / {property.District}"))
            .ToList();

        var units = await unitRepository.GetAdminUserOptionsAsync();

        return new AdminUserFormOptionsDto(roles, properties, units);
    }

    public async Task<AdminUserEditDataDto?> GetEditDataAsync(GetAdminUserEditDataInput input)
    {
        var user = await userManager.FindByIdAsync(input.UserId);
        if (!IsManagedInternalUser(user)) return null;
        var managedUser = user!;

        var selectedPropertyIds = await userPermissionScopeRepository
            .GetScopeIdsAsync(managedUser.Id, ScopeType.Property);
        var selectedUnitIds = await userPermissionScopeRepository
            .GetScopeIdsAsync(managedUser.Id, ScopeType.Unit);
        var roleId = await userRoleRepository.GetFirstRoleIdAsync(managedUser.Id) ?? 0;

        return new AdminUserEditDataDto(
            managedUser.Id,
            managedUser.AdSoyad ?? string.Empty,
            managedUser.Email ?? string.Empty,
            roleId,
            managedUser.IsActive,
            managedUser.Id == input.CurrentUserId,
            managedUser.TumTasinmazlaraErisim,
            selectedPropertyIds,
            selectedUnitIds,
            await GetFormOptionsAsync());
    }

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

        var (propertyScopeIds, unitScopeIds) = await GetValidatedScopeIdsAsync(
            input.HasAccessToAllProperties,
            input.PropertyIds,
            input.UnitIds);

        await userRoleService.RemoveAllRolesAsync(user.Id);
        await userRoleService.AddRoleByRolIdAsync(user.Id, input.RoleId, input.CurrentUserId);
        await permissionService.SetUserPermissionsAsync(user.Id, Array.Empty<string>());

        user.AdSoyad = input.FullName;
        user.TumTasinmazlaraErisim = input.HasAccessToAllProperties;
        await userManager.UpdateAsync(user);

        await SetScopeAsync(user.Id, propertyScopeIds, unitScopeIds);
    }

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

        var eventType = user.IsActive ? "User.Activated" : "User.Deactivated";
        await auditService.LogAsync(eventType, "ApplicationUser", user.Id, user.Email);
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

        var (validatedPropertyIds, validatedUnitIds) = await GetValidatedScopeIdsAsync(
            input.HasAccessToAllProperties,
            input.PropertyIds,
            input.UnitIds);
        var propertyIds = input.HasAccessToAllProperties ? null : validatedPropertyIds;
        var unitIds = input.HasAccessToAllProperties ? null : validatedUnitIds;

        await invitationService.SendAsync(new SendInvitationInput(
            input.Email,
            input.FullName,
            input.RoleId,
            input.InvitedByUserId,
            HasAccessToAllProperties: input.HasAccessToAllProperties,
            PropertyIds: propertyIds,
            UnitIds: unitIds));
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

    private async Task<(List<int> PropertyIds, List<int> UnitIds)> GetValidatedScopeIdsAsync(
        bool hasAccessToAllProperties,
        IReadOnlyList<int> propertyIds,
        IReadOnlyList<int> unitIds)
    {
        if (hasAccessToAllProperties) return ([], []);

        var distinctPropertyIds = propertyIds.Distinct().ToList();
        var distinctUnitIds = unitIds.Distinct().ToList();

        var validPropertyIds = (await propertyService.GetAllAsync(new GetPropertiesInput()))
            .Select(property => property.Id)
            .ToHashSet();
        Guard.InvalidField(
            distinctPropertyIds.Any(propertyId => !validPropertyIds.Contains(propertyId)),
            "SelectedPropertyIds",
            "Geçersiz taşınmaz seçildi.");

        var validUnitIds = (await unitRepository.GetAdminUserOptionsAsync())
            .Select(unit => unit.Id)
            .ToHashSet();
        Guard.InvalidField(
            distinctUnitIds.Any(unitId => !validUnitIds.Contains(unitId)),
            "SelectedUnitIds",
            "Geçersiz birim seçildi.");

        return (distinctPropertyIds, distinctUnitIds);
    }

    private static bool IsManagedInternalUser(ApplicationUser? user)
        => user is { IsSuperAdmin: false, UserType: UserType.Internal, TenantId: null };

    private async Task SetScopeAsync(string userId, List<int> propertyIds, List<int> unitIds)
    {
        await userPermissionScopeRepository.ReplaceAsync(userId, propertyIds, unitIds);
        await unitOfWork.SaveChangesAsync();

        scopeCache.Invalidate(userId);

        var parts = new List<string>();
        if (propertyIds.Count > 0) parts.Add($"{propertyIds.Count} taşınmaz");
        if (unitIds.Count > 0) parts.Add($"{unitIds.Count} birim");

        var detail = parts.Count > 0 ? $"Kapsam: {string.Join(", ", parts)}" : "Kapsam temizlendi";

        await auditService.LogAsync("User.ScopeChanged", "ApplicationUser", userId, detail);
    }
}

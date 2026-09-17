using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Identity;

namespace KiraTakip.Services.Identity;

public class UserRoleService(
    IUserRoleRepository userRoleRepository,
    IRoleRepository roleRepository,
    IApplicationUserRepository applicationUserRepository,
    IUnitOfWork unitOfWork,
    IUserPermissionCacheInvalidator permissionCacheInvalidator) : IUserRoleService
{
    public async Task<IList<string>> GetUserRolesAsync(string userId)
        => await userRoleRepository.GetRoleNamesAsync(userId);

    public Task<bool> IsInRoleAsync(string userId, string roleName)
        => userRoleRepository.IsInRoleAsync(userId, roleName);

    public async Task AddRoleByNameAsync(string userId, string roleName, string? atayanUserId = null)
    {
        var targetUser = Guard.NotFound(
            await applicationUserRepository.GetByIdAsync(userId),
            "Kullanıcı bulunamadı.");

        var rol = Guard.NotFound(
            await roleRepository.GetActiveInternalByNameAsync(roleName),
            $"Rol bulunamadı: {roleName}");

        await ValidateRoleAssignmentAsync(targetUser, rol, atayanUserId);

        var existingUserRole = await userRoleRepository.GetByUserAndRoleIdIgnoringFiltersAsync(userId, rol.Id);
        if (existingUserRole != null)
        {
            if (!existingUserRole.IsDeleted) return;
            existingUserRole.IsDeleted = false;
        }
        else
        {
            await userRoleRepository.AddAsync(new UserRole { UserId = userId, RoleId = rol.Id });
        }

        await unitOfWork.SaveChangesAsync();
        permissionCacheInvalidator.InvalidateAfterCommit(userId);
    }

    public async Task RemoveRoleByNameAsync(string userId, string roleName)
    {
        var userRole = await userRoleRepository.GetByUserAndRoleNameAsync(userId, roleName);
        if (userRole == null) return;
        userRole.IsDeleted = true;
        await unitOfWork.SaveChangesAsync();
        permissionCacheInvalidator.InvalidateAfterCommit(userId);
    }

    public async Task RemoveAllRolesAsync(string userId)
    {
        var userRoles = await userRoleRepository.GetAllByUserIgnoringFiltersAsync(userId);
        if (userRoles.Count == 0) return;
        userRoleRepository.RemoveRange(userRoles);
        await unitOfWork.SaveChangesAsync();
        permissionCacheInvalidator.InvalidateAfterCommit(userId);
    }

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName)
    {
        var userIds = await userRoleRepository.GetUserIdsByRoleNameAsync(roleName);
        return await applicationUserRepository.GetByIdsAsync(userIds);
    }

    public async Task<IList<string>> GetUserPermissionsFromRolesAsync(string userId)
        => await userRoleRepository.GetPermissionsAsync(userId);

    public async Task AddRoleByRolIdAsync(
        string userId,
        int rolId,
        string? atayanUserId = null,
        RoleAssignmentOperation operation = RoleAssignmentOperation.UserEdit)
    {
        var targetUser = Guard.NotFound(
            await applicationUserRepository.GetByIdAsync(userId),
            "Kullanıcı bulunamadı.");

        var rol = Guard.NotFound(
            await roleRepository.GetAsync(r => r.Id == rolId && !r.IsDeleted && r.IsActive),
            "Rol bulunamadı.");

        await ValidateRoleAssignmentAsync(targetUser, rol, atayanUserId, operation);

        var existingUserRole = await userRoleRepository.GetByUserAndRoleIdIgnoringFiltersAsync(userId, rolId);
        if (existingUserRole != null)
        {
            if (!existingUserRole.IsDeleted) return;
            existingUserRole.IsDeleted = false;
        }
        else
        {
            await userRoleRepository.AddAsync(new UserRole { UserId = userId, RoleId = rolId });
        }

        await unitOfWork.SaveChangesAsync();
        permissionCacheInvalidator.InvalidateAfterCommit(userId);
    }


    private async Task ValidateRoleAssignmentAsync(
        ApplicationUser targetUser,
        Role role,
        string? atayanUserId,
        RoleAssignmentOperation operation = RoleAssignmentOperation.UserEdit)
    {
        if (targetUser.UserType == UserType.Internal)
        {
            Guard.InvalidField(
                role.Scope != RoleScope.Internal,
                nameof(role.Scope),
                "İç kullanıcılara yalnızca iç roller atanabilir.");
        }
        else if (targetUser.UserType == UserType.Tenant)
        {
            Guard.InvalidField(
                role.Scope != RoleScope.Tenant,
                nameof(role.Scope),
                "Kiracı kullanıcılarına yalnızca kiracı rolleri atanabilir.");

            if (role.TenantId.HasValue)
            {
                Guard.InvalidField(
                    role.TenantId.Value != targetUser.TenantId,
                    nameof(role.TenantId),
                    "Farklı bir kiracıya ait özel rol bu kullanıcıya atanamaz.");
            }
        }

        Guard.InvalidField(
            string.IsNullOrWhiteSpace(atayanUserId),
            nameof(atayanUserId),
            "Rol atayan aktör belirtilmelidir.");

        {
            var assignerUser = Guard.NotFound(
                await applicationUserRepository.GetByIdAsync(atayanUserId!),
                "Rol atayan kullanıcı bulunamadı.");

            if (role.Name == RoleNames.KiraciYoneticisi && role.IsSystemRole)
            {
                Guard.Forbidden(
                    assignerUser.UserType == UserType.Tenant,
                    "Kiracı kullanıcıları Kiracı Yöneticisi rolünü atayamaz.");

                if (!assignerUser.IsSuperAdmin)
                {
                    Guard.Forbidden(
                        assignerUser.UserType != UserType.Internal,
                        "Kiracı Yöneticisi rolünü yalnızca yetkili iç kullanıcılar atayabilir.");

                    var assignerPermissions = await userRoleRepository.GetPermissionsAsync(assignerUser.Id);
                    var (modulePermission, actionPermission) = operation switch
                    {
                        RoleAssignmentOperation.Invitation =>
                            (PermissionCatalog.Invitation.Module, PermissionCatalog.Invitation.Create),
                        _ => (PermissionCatalog.User.Module, PermissionCatalog.User.Edit)
                    };
                    var hasAssignmentPermission = assignerPermissions.Contains(modulePermission)
                        && assignerPermissions.Contains(actionPermission);

                    Guard.Forbidden(
                        !hasAssignmentPermission,
                        "Kiracı Yöneticisi rolünü atamak için gerekli işlem yetkiniz bulunmamaktadır.");
                }
            }
        }
    }
}

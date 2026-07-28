using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class UserRoleService(
    IUserRoleRepository userRoleRepository,
    IRoleRepository roleRepository,
    IApplicationUserRepository applicationUserRepository,
    IUnitOfWork unitOfWork) : IUserRoleService
{
    public async Task<IList<string>> GetUserRolesAsync(string userId)
        => await userRoleRepository.GetRoleNamesAsync(userId);

    public Task<bool> IsInRoleAsync(string userId, string roleName)
        => userRoleRepository.IsInRoleAsync(userId, roleName);

    public async Task AddRoleByNameAsync(string userId, string roleName, string? atayanUserId = null)
    {
        var rol = Guard.NotFound(
            await roleRepository.GetActiveInternalByNameAsync(roleName),
            $"Rol bulunamadı: {roleName}");

        if (await userRoleRepository.ExistsIgnoringFiltersAsync(userId, rol.Id)) return;

        await userRoleRepository.AddAsync(new UserRole { UserId = userId, RoleId = rol.Id });
        await unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveRoleByNameAsync(string userId, string roleName)
    {
        var userRole = await userRoleRepository.GetByUserAndRoleNameAsync(userId, roleName);
        if (userRole == null) return;
        userRole.IsDeleted = true;
        await unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveAllRolesAsync(string userId)
    {
        var userRoles = await userRoleRepository.GetAllByUserIgnoringFiltersAsync(userId);
        userRoleRepository.RemoveRange(userRoles);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName)
    {
        var userIds = await userRoleRepository.GetUserIdsByRoleNameAsync(roleName);
        return await applicationUserRepository.GetByIdsAsync(userIds);
    }

    public async Task<IList<string>> GetUserPermissionsFromRolesAsync(string userId)
        => await userRoleRepository.GetPermissionsAsync(userId);

    public async Task AddRoleByRolIdAsync(string userId, int rolId, string? atayanUserId = null)
    {
        if (await userRoleRepository.ExistsIgnoringFiltersAsync(userId, rolId)) return;

        await userRoleRepository.AddAsync(new UserRole { UserId = userId, RoleId = rolId });
        await unitOfWork.SaveChangesAsync();
    }
}

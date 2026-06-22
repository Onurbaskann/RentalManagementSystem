namespace KiraTakip.Services.Interfaces;

public interface IUserRolService
{
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<bool> IsInRoleAsync(string userId, string roleName);
    Task AddRoleByNameAsync(string userId, string roleName, string? atayanUserId = null);
    Task RemoveRoleByNameAsync(string userId, string roleName);
    Task RemoveAllRolesAsync(string userId);
    Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName);
    Task<IList<string>> GetUserPermissionsFromRolesAsync(string userId);
    Task AddRoleByRolIdAsync(string userId, int rolId, string? atayanUserId = null);
}

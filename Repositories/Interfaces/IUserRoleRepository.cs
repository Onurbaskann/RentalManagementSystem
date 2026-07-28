using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface IUserRoleRepository : IBaseRepository<UserRole>
{
    Task<int> CountUsersInRoleAsync(int roleId);
    Task<int> CountUsersInRoleForTenantAsync(int roleId, int tenantId);
    Task<bool> HasAnyUsersInRoleAsync(int roleId);
    Task<int?> GetFirstRoleIdAsync(string userId, CancellationToken ct = default);
    Task<(int RoleId, string RoleName)?> GetUserRoleInfoAsync(string userId, CancellationToken ct = default);
    Task<List<string>> GetRoleNamesAsync(string userId, CancellationToken ct = default);
    Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken ct = default);
    Task<UserRole?> GetByUserAndRoleNameAsync(string userId, string roleName, CancellationToken ct = default);
    Task<List<UserRole>> GetAllByUserIgnoringFiltersAsync(string userId, CancellationToken ct = default);
    Task<List<string>> GetUserIdsByRoleNameAsync(string roleName, CancellationToken ct = default);
    Task<List<string>> GetPermissionsAsync(string userId, CancellationToken ct = default);
    Task<bool> ExistsIgnoringFiltersAsync(string userId, int roleId, CancellationToken ct = default);
    Task<List<string>> GetUserIdsByRoleIdAsync(int roleId, CancellationToken ct = default);
    void RemoveRange(IEnumerable<UserRole> userRoles);
}

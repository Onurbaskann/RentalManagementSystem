using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Identity;

public interface IRolePermissionRepository : IRepository<RolePermission, int>
{
    Task<List<RolePermission>> GetForRoleAsync(int roleId);
    Task<List<string>> GetPermissionsForRoleAsync(int roleId);
    Task RemoveRangeAsync(IEnumerable<RolePermission> entities);
    Task AddRangeAsync(IEnumerable<RolePermission> entities);
}

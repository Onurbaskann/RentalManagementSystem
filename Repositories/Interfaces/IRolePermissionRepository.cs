using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface IRolePermissionRepository
{
    Task<List<RolePermission>> GetForRoleAsync(int roleId);
    Task<List<string>> GetPermissionsForRoleAsync(int roleId);
    Task RemoveRangeAsync(IEnumerable<RolePermission> entities);
    Task AddRangeAsync(IEnumerable<RolePermission> entities);
}

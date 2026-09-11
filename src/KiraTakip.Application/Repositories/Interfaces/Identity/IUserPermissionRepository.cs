using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Identity;

public interface IUserPermissionRepository : IRepositoryBase<UserPermission>
{
    Task<List<string>> GetUserPermissionsAsync(string userId);
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task<List<UserPermission>> GetForUserAsync(string userId);
    Task RemoveRangeAsync(IEnumerable<UserPermission> entities);
    Task AddRangeAsync(IEnumerable<UserPermission> entities);
}

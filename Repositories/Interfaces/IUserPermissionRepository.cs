namespace KiraTakip.Repositories.Interfaces;

public interface IUserPermissionRepository : IRepositoryBase<UserPermission>
{
    Task<List<string>> GetUserPermissionsAsync(string userId);
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task<List<UserPermission>> GetForUserAsync(string userId);
    Task RemoveRangeAsync(IEnumerable<UserPermission> entities);
    Task AddRangeAsync(IEnumerable<UserPermission> entities);
}

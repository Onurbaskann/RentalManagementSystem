using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PermissionService : IPermissionService
{
    private readonly IUserPermissionRepository _repo;
    private readonly IUnitOfWork _uow;

    public PermissionService(IUserPermissionRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<IList<string>> GetUserPermissionsAsync(string userId)
        => await _repo.GetUserPermissionsAsync(userId);

    public async Task<bool> HasPermissionAsync(string userId, string permission)
        => await _repo.HasPermissionAsync(userId, permission);

    public async Task SetUserPermissionsAsync(string userId, IEnumerable<string> permissions)
    {
        var permissionList = permissions.ToList();

        var existing = await _repo.GetForUserAsync(userId);
        await _repo.RemoveRangeAsync(existing);
        await _uow.SaveChangesAsync();

        var newPermissions = permissionList.Select(p => new UserPermission
        {
            UserId = userId,
            Permission = p,
        });

        await _repo.AddRangeAsync(newPermissions);
        await _uow.SaveChangesAsync();
    }
}

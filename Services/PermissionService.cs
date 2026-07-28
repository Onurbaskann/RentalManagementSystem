using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PermissionService(
    IUserPermissionRepository userPermissionRepository,
    IUnitOfWork uow) : IPermissionService
{
    public async Task<IList<string>> GetUserPermissionsAsync(string userId)
        => await userPermissionRepository.GetUserPermissionsAsync(userId);

    public async Task<bool> HasPermissionAsync(string userId, string permission)
        => await userPermissionRepository.HasPermissionAsync(userId, permission);

    public async Task SetUserPermissionsAsync(string userId, IEnumerable<string> permissions)
    {
        var permissionList = permissions.ToList();

        var existing = await userPermissionRepository.GetForUserAsync(userId);
        await userPermissionRepository.RemoveRangeAsync(existing);
        await uow.SaveChangesAsync();

        var newPermissions = permissionList.Select(p => new UserPermission
        {
            UserId = userId,
            Permission = p,
        });

        await userPermissionRepository.AddRangeAsync(newPermissions);
        await uow.SaveChangesAsync();
    }
}

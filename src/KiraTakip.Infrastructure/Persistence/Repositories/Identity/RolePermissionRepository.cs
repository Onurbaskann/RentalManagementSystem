using KiraTakip.Data;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Identity;

public class RolePermissionRepository(ApplicationDbContext ctx)
    : Repository<RolePermission, int>(ctx, permission => permission.Id), IRolePermissionRepository
{
    public async Task<List<RolePermission>> GetForRoleAsync(int roleId)
        => await _ctx.RolPermissions.Where(rp => rp.RoleId == roleId).ToListAsync();

    public async Task<List<string>> GetPermissionsForRoleAsync(int roleId)
        => await _ctx.RolPermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .ToListAsync();

    public Task RemoveRangeAsync(IEnumerable<RolePermission> entities)
    {
        _ctx.RolPermissions.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task AddRangeAsync(IEnumerable<RolePermission> entities)
    {
        await _ctx.RolPermissions.AddRangeAsync(entities);
    }
}

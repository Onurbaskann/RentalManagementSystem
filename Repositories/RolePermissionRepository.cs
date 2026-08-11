using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

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

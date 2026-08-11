using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UserPermissionRepository(ApplicationDbContext ctx)
    : RepositoryBase<UserPermission>(ctx), IUserPermissionRepository
{
    public async Task<List<string>> GetUserPermissionsAsync(string userId)
        => await _ctx.UserPermissions
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

    public async Task<bool> HasPermissionAsync(string userId, string permission)
        => await _ctx.UserPermissions
            .AsNoTracking()
            .AnyAsync(p => p.UserId == userId && p.Permission == permission);

    public async Task<List<UserPermission>> GetForUserAsync(string userId)
        => await _ctx.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();

    public Task RemoveRangeAsync(IEnumerable<UserPermission> entities)
    {
        _ctx.UserPermissions.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task AddRangeAsync(IEnumerable<UserPermission> entities)
        => await _ctx.UserPermissions.AddRangeAsync(entities);
}

using KiraTakip.Data;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

// Faz 4'te tam implemente edilecek. Şu an DB hazır değil.
public class PermissionService(ApplicationDbContext context) : IPermissionService
{
    public async Task<IList<string>> GetUserPermissionsAsync(string userId)
    {
        return await context.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        return await context.UserPermissions
            .AnyAsync(p => p.UserId == userId && p.Permission == permission);
    }

    public async Task SetUserPermissionsAsync(string userId, IEnumerable<string> permissions, string grantedByUserId)
    {
        var permissionList = permissions.ToList();

        var existing = await context.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();

        context.UserPermissions.RemoveRange(existing);

        var now = DateTime.UtcNow;
        var newPermissions = permissionList.Select(p => new UserPermission
        {
            UserId      = userId,
            Permission  = p,
            GrantedBy   = grantedByUserId,
            GrantedAt   = now,
        });

        await context.UserPermissions.AddRangeAsync(newPermissions);
        await context.SaveChangesAsync();
    }
}

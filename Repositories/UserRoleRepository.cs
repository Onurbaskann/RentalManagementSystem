using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UserRoleRepository : RepositoryBase<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<int> CountUsersInRoleAsync(int roleId)
        => await _dbSet.CountAsync(ur => ur.RoleId == roleId);

    public async Task<int> CountUsersInRoleForTenantAsync(int roleId, int tenantId)
        => await _dbSet.CountAsync(ur => ur.RoleId == roleId &&
                                         _ctx.Users.Any(u => u.Id == ur.UserId && u.TenantId == tenantId));

    public async Task<bool> HasAnyUsersInRoleAsync(int roleId)
        => await _dbSet.AnyAsync(ur => ur.RoleId == roleId);

    public async Task<int?> GetFirstRoleIdAsync(string userId, CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => (int?)userRole.RoleId)
            .FirstOrDefaultAsync(ct);

    public async Task<(int RoleId, string RoleName)?> GetUserRoleInfoAsync(string userId, CancellationToken ct = default)
    {
        var result = await _dbSet
            .Where(ur => ur.UserId == userId)
            .Join(_ctx.Roller, ur => ur.RoleId, r => r.Id, (ur, r) => new { r.Id, r.Name })
            .FirstOrDefaultAsync(ct);

        if (result == null) return null;
        return (result.Id, result.Name);
    }

    public Task<List<string>> GetRoleNamesAsync(string userId, CancellationToken ct = default)
        => _dbSet.Where(userRole => userRole.UserId == userId && userRole.Role!.IsActive && !userRole.Role.IsDeleted)
            .Select(userRole => userRole.Role!.Name).ToListAsync(ct);

    public Task<bool> IsInRoleAsync(string userId, string roleName, CancellationToken ct = default)
        => _dbSet.AnyAsync(userRole => userRole.UserId == userId
            && userRole.Role!.Name == roleName
            && userRole.Role.IsActive
            && !userRole.Role.IsDeleted, ct);

    public Task<UserRole?> GetByUserAndRoleNameAsync(string userId, string roleName, CancellationToken ct = default)
        => _dbSet.Include(userRole => userRole.Role)
            .FirstOrDefaultAsync(userRole => userRole.UserId == userId && userRole.Role!.Name == roleName, ct);

    public Task<List<UserRole>> GetAllByUserIgnoringFiltersAsync(string userId, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters().Where(userRole => userRole.UserId == userId).ToListAsync(ct);

    public Task<List<string>> GetUserIdsByRoleNameAsync(string roleName, CancellationToken ct = default)
        => _dbSet.Where(userRole => userRole.Role!.Name == roleName && userRole.Role.IsActive && !userRole.Role.IsDeleted)
            .Select(userRole => userRole.UserId).ToListAsync(ct);

    public Task<List<string>> GetPermissionsAsync(string userId, CancellationToken ct = default)
        => _dbSet.Where(userRole => userRole.UserId == userId && userRole.Role!.IsActive && !userRole.Role.IsDeleted)
            .SelectMany(userRole => userRole.Role!.RolePermissions.Select(permission => permission.Permission))
            .Distinct().ToListAsync(ct);

    public Task<bool> ExistsIgnoringFiltersAsync(string userId, int roleId, CancellationToken ct = default)
        => _dbSet.IgnoreQueryFilters().AnyAsync(userRole => userRole.UserId == userId && userRole.RoleId == roleId, ct);

    public Task<List<string>> GetUserIdsByRoleIdAsync(int roleId, CancellationToken ct = default)
        => _dbSet.AsNoTracking().Where(userRole => userRole.RoleId == roleId).Select(userRole => userRole.UserId).ToListAsync(ct);

    public void RemoveRange(IEnumerable<UserRole> userRoles) => _dbSet.RemoveRange(userRoles);
}

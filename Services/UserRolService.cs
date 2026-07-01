using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class UserRolService : IUserRolService
{
    private readonly ApplicationDbContext _db;

    public UserRolService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
        => await (from ur in _db.UserRoller
                  join r in _db.Roller on ur.RolId equals r.Id
                  where ur.UserId == userId && r.IsActive && !r.IsDeleted
                  select r.Ad).ToListAsync();

    public async Task<bool> IsInRoleAsync(string userId, string roleName)
        => await (from ur in _db.UserRoller
                  join r in _db.Roller on ur.RolId equals r.Id
                  where ur.UserId == userId && r.Ad == roleName && r.IsActive && !r.IsDeleted
                  select ur.Id).AnyAsync();

    public async Task AddRoleByNameAsync(string userId, string roleName, string? atayanUserId = null)
    {
        var rol = await _db.Roller
            .FirstOrDefaultAsync(r => r.Ad == roleName && r.Scope == RolScope.Internal && r.IsActive && !r.IsDeleted);
        if (rol == null)
            throw new InvalidOperationException($"Rol bulunamadı: {roleName}");

        var mevcutMu = await _db.UserRoller.AnyAsync(ur => ur.UserId == userId && ur.RolId == rol.Id);
        if (mevcutMu) return;

        _db.UserRoller.Add(new UserRol
        {
            UserId = userId,
            RolId = rol.Id,
        });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveRoleByNameAsync(string userId, string roleName)
    {
        var userRol = await (from ur in _db.UserRoller
                             join r in _db.Roller on ur.RolId equals r.Id
                             where ur.UserId == userId && r.Ad == roleName
                             select ur).FirstOrDefaultAsync();
        if (userRol == null) return;
        userRol.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAllRolesAsync(string userId)
    {
        var userRoller = await _db.UserRoller.IgnoreQueryFilters()
            .Where(ur => ur.UserId == userId).ToListAsync();
        _db.UserRoller.RemoveRange(userRoller);
        await _db.SaveChangesAsync();
    }

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName)
    {
        var userIds = await (from ur in _db.UserRoller
                             join r in _db.Roller on ur.RolId equals r.Id
                             where r.Ad == roleName && r.IsActive && !r.IsDeleted
                             select ur.UserId).ToListAsync();

        return await _db.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
    }

    public async Task<IList<string>> GetUserPermissionsFromRolesAsync(string userId)
        => await (from ur in _db.UserRoller
                  join r in _db.Roller on ur.RolId equals r.Id
                  join rp in _db.RolPermissions on r.Id equals rp.RolId
                  where ur.UserId == userId && r.IsActive && !r.IsDeleted
                  select rp.Permission).Distinct().ToListAsync();

    public async Task AddRoleByRolIdAsync(string userId, int rolId, string? atayanUserId = null)
    {
        var mevcutMu = await _db.UserRoller.IgnoreQueryFilters()
            .AnyAsync(ur => ur.UserId == userId && ur.RolId == rolId);
        if (mevcutMu) return;

        _db.UserRoller.Add(new UserRol
        {
            UserId = userId,
            RolId = rolId,
        });
        await _db.SaveChangesAsync();
    }
}

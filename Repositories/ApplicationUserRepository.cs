using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KiraTakip.Repositories;

public class ApplicationUserRepository(ApplicationDbContext ctx) : IApplicationUserRepository
{
    public async Task<List<AdminUserAccountDto>> GetInternalAdminUsersAsync(CancellationToken ct = default)
        => await ctx.Users.AsNoTracking()
            .Where(user => user.TenantId == null && !user.IsSuperAdmin)
            .OrderBy(user => user.AdSoyad)
            .Select(user => new AdminUserAccountDto(
                user.Id,
                user.AdSoyad,
                user.Email,
                user.IsActive))
            .ToListAsync(ct);

    public async Task<List<AdminTenantUserAccountDto>> GetAdminTenantUsersAsync(CancellationToken ct = default)
        => await ctx.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.TenantId != null)
            .OrderBy(user => user.AdSoyad)
            .Select(user => new AdminTenantUserAccountDto(
                user.Id,
                user.AdSoyad,
                user.Email,
                user.TenantId!.Value,
                ctx.Tenants.IgnoreQueryFilters()
                    .Where(tenant => tenant.Id == user.TenantId)
                    .Select(tenant => tenant.DisplayName)
                    .FirstOrDefault(),
                user.IsActive))
            .ToListAsync(ct);

    public async Task<List<ApplicationUser>> GetUsersByTenantIdAsync(int tenantId, bool ignoreQueryFilters = false, CancellationToken ct = default)
    {
        var query = ctx.Users.AsQueryable();
        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.AdSoyad)
            .ToListAsync(ct);
    }

    public async Task<ApplicationUser?> GetUserByIdAndTenantIdAsync(string userId, int tenantId, bool ignoreQueryFilters = false, CancellationToken ct = default)
    {
        var query = ctx.Users.AsQueryable();
        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct);
    }

    public Task<List<TenantUserListItemDto>> GetTenantUserListAsync(
        int tenantId,
        CancellationToken ct = default)
        => ctx.Users.AsNoTracking()
            .Where(user => user.TenantId == tenantId)
            .OrderBy(user => user.AdSoyad)
            .Select(user => new TenantUserListItemDto(
                user.Id,
                user.AdSoyad ?? user.Email ?? "—",
                user.Email ?? "—",
                ctx.UserRoller
                    .Where(userRole => userRole.UserId == user.Id)
                    .Select(userRole => userRole.Role!.Name)
                    .FirstOrDefault() ?? "—",
                ctx.UserRoller
                    .Where(userRole => userRole.UserId == user.Id)
                    .Select(userRole => userRole.RoleId)
                    .FirstOrDefault(),
                user.IsActive))
            .ToListAsync(ct);

    public Task<TenantUserEditCoreDto?> GetTenantUserForEditAsync(
        string userId,
        int tenantId,
        CancellationToken ct = default)
        => ctx.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.TenantId == tenantId)
            .Select(user => new TenantUserEditCoreDto(
                user.Id,
                user.AdSoyad ?? string.Empty,
                user.Email ?? string.Empty,
                user.IsActive,
                ctx.UserRoller
                    .Where(userRole => userRole.UserId == user.Id)
                    .Select(userRole => userRole.RoleId)
                    .FirstOrDefault(),
                ctx.UserRoller
                    .Where(userRole => userRole.UserId == user.Id)
                    .Select(userRole => userRole.Role!.Name)
                    .FirstOrDefault() ?? string.Empty))
            .FirstOrDefaultAsync(ct);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct = default)
        => ctx.Users.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, ct);

    public Task<UserScopeAccountDto?> GetScopeAccountAsync(
        string userId,
        CancellationToken ct = default)
        => ctx.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserScopeAccountDto(
                user.TumTasinmazlaraErisim || user.IsSuperAdmin))
            .FirstOrDefaultAsync(ct);
    public async Task<bool> HasTenantManagerAsync(
        int tenantId,
        string? excludedUserId = null,
        int? excludedRoleId = null,
        CancellationToken ct = default)
    {
        var query = from u in ctx.Users
                    join ur in ctx.UserRoller on u.Id equals ur.UserId
                    join r in ctx.Roller on ur.RoleId equals r.Id
                    where u.TenantId == tenantId
                          && u.IsActive
                          && r.IsActive
                          && !r.IsDeleted
                          && r.Name == RoleNames.KiraciYoneticisi
                    select new { UserId = u.Id, RolId = r.Id };

        if (excludedUserId != null)
            query = query.Where(x => x.UserId != excludedUserId);

        if (excludedRoleId != null)
            query = query.Where(x => x.RolId != excludedRoleId);

        return await query.AnyAsync(ct);
    }

    public Task<Dictionary<string, string?>> GetDisplayNamesAsync(IReadOnlyCollection<string> userIds, CancellationToken ct = default)
        => ctx.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.AdSoyad ?? user.Email, ct);

    public Task<List<ApplicationUser>> GetByIdsAsync(IReadOnlyCollection<string> userIds, CancellationToken ct = default)
        => ctx.Users.Where(user => userIds.Contains(user.Id)).ToListAsync(ct);
}

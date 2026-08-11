using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KiraTakip.Repositories;

public class RoleRepository : RepositoryBase<Role>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext ctx) : base(ctx) { }

    public Task<PagedResult<RoleListItemDto>> GetInternalRolesWithDetailsPagedAsync(
        TableQuery tableQuery,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking()
            .Where(role => role.Scope == RoleScope.Internal && !role.IsDeleted);
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(role => role.Name.Contains(search)
                || (role.Description != null && role.Description.Contains(search)));
        }
        var items = query
            .OrderBy(role => role.IsSystemRole ? 0 : 1)
            .ThenBy(role => role.Name)
            .ThenBy(role => role.Id)
            .Select(role => new RoleListItemDto(
                role.Id,
                role.Name,
                role.Description,
                role.IsSystemRole,
                role.IsActive,
                _ctx.UserRoller.Count(userRole => userRole.RoleId == role.Id),
                role.RolePermissions.Count));
        return GetPagedResultAsync(query, items, tableQuery, ct);
    }

    public Task<PagedResult<RoleListItemDto>> GetTenantRolesWithDetailsPagedAsync(
        int tenantId,
        TableQuery tableQuery,
        CancellationToken ct = default)
    {
        var query = _dbSet.AsNoTracking()
            .Where(role => role.Scope == RoleScope.Tenant
                && (role.TenantId == null || role.TenantId == tenantId)
                && role.IsActive
                && !role.IsDeleted);
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(role => role.Name.Contains(search)
                || (role.Description != null && role.Description.Contains(search)));
        }
        var items = query
            .OrderBy(role => role.IsSystemRole ? 0 : 1)
            .ThenBy(role => role.Name)
            .ThenBy(role => role.Id)
            .Select(role => new RoleListItemDto(
                role.Id,
                role.Name,
                role.Description,
                role.IsSystemRole,
                role.IsActive,
                _ctx.UserRoller.Count(userRole => userRole.RoleId == role.Id
                    && _ctx.Users.Any(user => user.Id == userRole.UserId && user.TenantId == tenantId)),
                role.RolePermissions.Count));
        return GetPagedResultAsync(query, items, tableQuery, ct);
    }

    public async Task<List<AdminUserRoleOptionDto>> GetActiveInternalRoleOptionsAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
            .Where(role => role.Scope == RoleScope.Internal && role.IsActive && !role.IsDeleted)
            .OrderBy(role => role.IsSystemRole ? 0 : 1)
            .ThenBy(role => role.Name)
            .Select(role => new AdminUserRoleOptionDto(role.Id, role.Name))
            .ToListAsync(ct);

    public Task<Role?> GetActiveInternalByIdAsync(int roleId, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(role =>
            role.Id == roleId &&
            role.Scope == RoleScope.Internal &&
            role.IsActive &&
            !role.IsDeleted,
            ct);

    public async Task<List<Role>> GetActiveTenantRolesAsync(int tenantId, CancellationToken ct = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(r => r.Scope == RoleScope.Tenant && (r.TenantId == null || r.TenantId == tenantId) && r.IsActive && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<Role?> GetTenantRoleByIdAsync(int roleId, int tenantId, CancellationToken ct = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r =>
                r.Id == roleId &&
                r.Scope == RoleScope.Tenant &&
                (r.TenantId == null || r.TenantId == tenantId) &&
                r.IsActive &&
                !r.IsDeleted, ct);
    }

    public Task<List<RoleListItemDto>> GetTenantRolesWithDetailsAsync(
        int tenantId,
        CancellationToken ct = default)
        => _dbSet.AsNoTracking()
            .Where(role => role.Scope == RoleScope.Tenant
                && (role.TenantId == null || role.TenantId == tenantId)
                && role.IsActive
                && !role.IsDeleted)
            .OrderBy(role => role.IsSystemRole ? 0 : 1)
            .ThenBy(role => role.Name)
            .Select(role => new RoleListItemDto(
                role.Id,
                role.Name,
                role.Description,
                role.IsSystemRole,
                role.IsActive,
                _ctx.UserRoller.Count(userRole => userRole.RoleId == role.Id
                    && _ctx.Users.Any(user => user.Id == userRole.UserId
                        && user.TenantId == tenantId)),
                role.RolePermissions.Count))
            .ToListAsync(ct);

    public async Task<TenantRoleEditDto?> GetTenantRoleForEditAsync(
        int roleId,
        int tenantId,
        CancellationToken ct = default)
    {
        var role = await _dbSet.AsNoTracking()
            .Where(item => item.Id == roleId
                && item.Scope == RoleScope.Tenant
                && item.TenantId == tenantId
                && !item.IsDeleted)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Description,
                SelectedPermissions = item.RolePermissions
                    .Select(permission => permission.Permission)
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        return role == null
            ? null
            : new TenantRoleEditDto(
                role.Id,
                role.Name,
                role.Description,
                role.SelectedPermissions);
    }

    public Task<Role?> GetTenantOwnedByIdAsync(
        int roleId,
        int tenantId,
        CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(role => role.Id == roleId
            && role.Scope == RoleScope.Tenant
            && role.TenantId == tenantId
            && !role.IsDeleted,
            ct);

    public Task<Role?> GetActiveInternalByNameAsync(string roleName, CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(role => role.Name == roleName
            && role.Scope == RoleScope.Internal
            && role.IsActive
            && !role.IsDeleted, ct);
}

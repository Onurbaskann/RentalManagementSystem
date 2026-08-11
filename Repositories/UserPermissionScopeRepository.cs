using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UserPermissionScopeRepository(ApplicationDbContext ctx)
    : RepositoryBase<UserPermissionScope>(ctx), IUserPermissionScopeRepository
{
    public async Task<List<int>> GetScopeIdsAsync(
        string userId,
        ScopeType scopeType,
        CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
            .Where(scope => scope.UserId == userId && scope.ScopeType == scopeType && !scope.IsDeleted)
            .Select(scope => scope.ScopeId)
            .ToListAsync(ct);

    public async Task ReplaceAsync(
        string userId,
        IReadOnlyCollection<int> propertyIds,
        IReadOnlyCollection<int> unitIds,
        CancellationToken ct = default)
    {
        var existingScopes = await _dbSet
            .Where(scope => scope.UserId == userId &&
                (scope.ScopeType == ScopeType.Property || scope.ScopeType == ScopeType.Unit))
            .ToListAsync(ct);
        _dbSet.RemoveRange(existingScopes);

        var scopes = propertyIds.Select(propertyId => new UserPermissionScope
        {
            UserId = userId,
            ScopeType = ScopeType.Property,
            ScopeId = propertyId,
        }).Concat(unitIds.Select(unitId => new UserPermissionScope
        {
            UserId = userId,
            ScopeType = ScopeType.Unit,
            ScopeId = unitId,
        }));

        await _dbSet.AddRangeAsync(scopes, ct);
    }

    public async Task AddRangeAsync(IEnumerable<UserPermissionScope> scopes, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(scopes, ct);
}

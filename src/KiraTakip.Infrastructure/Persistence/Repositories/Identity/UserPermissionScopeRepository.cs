using KiraTakip.Data;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Identity;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Identity;

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
        IReadOnlyCollection<int> spaceIds,
        CancellationToken ct = default)
    {
        var existingScopes = await _dbSet
            .Where(scope => scope.UserId == userId)
            .ToListAsync(ct);
        _dbSet.RemoveRange(existingScopes);

        var scopes = spaceIds.Distinct().Select(spaceId => new UserPermissionScope
        {
            UserId = userId,
            ScopeType = ScopeType.Space,
            ScopeId = spaceId,
        });

        await _dbSet.AddRangeAsync(scopes, ct);
    }

    public async Task<List<string>> RemoveBySpaceAsync(int spaceId, CancellationToken ct = default)
    {
        var scopes = await _dbSet
            .Where(scope => scope.ScopeType == ScopeType.Space && scope.ScopeId == spaceId)
            .ToListAsync(ct);
        _dbSet.RemoveRange(scopes);
        return scopes.Select(scope => scope.UserId).Distinct().ToList();
    }

    public async Task AddRangeAsync(IEnumerable<UserPermissionScope> scopes, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(scopes, ct);
}

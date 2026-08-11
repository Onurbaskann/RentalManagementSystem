using KiraTakip.Models;
using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface IUserPermissionScopeRepository : IRepositoryBase<UserPermissionScope>
{
    Task<List<int>> GetScopeIdsAsync(string userId, ScopeType scopeType, CancellationToken ct = default);
    Task ReplaceAsync(
        string userId,
        IReadOnlyCollection<int> propertyIds,
        IReadOnlyCollection<int> unitIds,
        CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<UserPermissionScope> scopes, CancellationToken ct = default);
}

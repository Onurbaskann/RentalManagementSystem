using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Identity;

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

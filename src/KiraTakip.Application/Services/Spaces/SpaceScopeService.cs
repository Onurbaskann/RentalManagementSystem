using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Spaces;

namespace KiraTakip.Services.Spaces;

public class SpaceScopeService(ISpaceRepository spaces) : ISpaceScopeService
{
    public async Task<SpaceAccessScope> BuildAsync(bool hasGlobalAccess, IReadOnlyCollection<int> scopeSpaceIds, CancellationToken ct = default)
    {
        if (hasGlobalAccess || scopeSpaceIds.Count == 0)
            return new SpaceAccessScope(hasGlobalAccess, []);

        var paths = (await spaces.GetPathsAsync(scopeSpaceIds, ct)).Distinct(StringComparer.Ordinal).ToList();
        var roots = paths
            .Where(p => !paths.Any(o => o != p && SpaceTreePolicy.IsAncestorOrSelf(o, p)))
            .ToList();
        return new SpaceAccessScope(false, roots);
    }
}

using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces.Spaces;

public interface ISpaceScopeService
{
    Task<SpaceAccessScope> BuildAsync(bool hasGlobalAccess, IReadOnlyCollection<int> scopeSpaceIds, CancellationToken ct = default);
}

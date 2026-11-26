using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Spaces;

public interface ISpaceAttributeRepository : IRepositoryBase<SpaceAttributeDefinition>
{
    Task<List<SpaceAttributeDefinition>> GetAllOrderedAsync();
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<bool> IsUsedAsync(int definitionId);
    Task<List<SpaceAttributeDefinition>> GetByIdsAsync(IReadOnlyCollection<int> ids);
    Task<List<SpaceAttributeDefinition>> GetTypeDefaultsAsync(int spaceTypeId);
    Task<List<SpaceAttributeValue>> GetValuesForUpdateAsync(int spaceId);
    void AddValue(SpaceAttributeValue value);
}

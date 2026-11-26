using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Spaces;

public interface ISpaceTypeRepository : IRepositoryBase<SpaceType>
{
    Task<List<SpaceType>> GetAllOrderedAsync();
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<List<SpaceTypeDefaultAttribute>> GetDefaultsAsync(int spaceTypeId);
    Task<Dictionary<int, List<int>>> GetDefaultAttributeIdsAsync();
    void AddDefault(SpaceTypeDefaultAttribute item);
}

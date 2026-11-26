using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Spaces;

public interface ISpaceRateRepository : IRepositoryBase<SpaceRate>
{
    Task<List<SpaceRate>> GetBySpaceAsync(int spaceId);
    Task<SpaceRate?> GetByKeyAsync(int spaceId, int tenantCategoryId, int chargeTypeId);
}

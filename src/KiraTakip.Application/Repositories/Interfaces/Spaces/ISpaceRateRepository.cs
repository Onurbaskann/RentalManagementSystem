using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Spaces;

public interface ISpaceRateRepository : IRepositoryBase<SpaceRate>
{
    Task<List<SpaceRate>> GetBySpaceAsync(int spaceId);
    Task<List<(int SpaceId, KiraTakip.Models.Dtos.RateValueDto Rate)>> GetActiveRatesAsync(
        IReadOnlyCollection<int> spaceIds, int tenantCategoryId, int chargeTypeId);
    Task<SpaceRate?> GetByKeyAsync(int spaceId, int tenantCategoryId, int chargeTypeId);
}

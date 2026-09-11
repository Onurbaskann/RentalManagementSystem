using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Property;

namespace KiraTakip.Repositories.Interfaces.Properties;

public interface IUnitTypeRepository : IRepositoryBase<UnitType>
{
    Task<List<UnitTypeListItemDto>> GetListAsync();
    Task<PagedResult<UnitTypeListItemDto>> GetPagedListAsync(TableQuery query);
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);

    // Cross-aggregate kontroller (ChargeType DurumDegistir + UnitType DurumDegistir için)
    Task<bool> AnyAktifByBorcTipiIdAsync(int chargeTypeId, int? excludeId = null);
    Task<List<UnitTypeOptionDto>> GetActiveOptionsAsync();
    Task<List<UnitTypeUsageDto>> GetActiveUsagesAsync(IReadOnlyCollection<int> unitTypeIds);
}

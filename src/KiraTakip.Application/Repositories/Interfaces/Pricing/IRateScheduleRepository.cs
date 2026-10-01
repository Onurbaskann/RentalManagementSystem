using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.RateHierarchy;
using KiraTakip.Models.Dtos.RateSchedule;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Pricing;

public interface IRateScheduleRepository : IRepositoryBase<RateSchedule>
{
    Task<PagedResult<RateYearSummaryDto>> GetYearSummariesPagedAsync(TableQuery query);
    Task<RateValueDto?> GetRateAsync(int kategoriId, int chargeTypeId, int donemYil);
    // Toplu (bulk) hali — GetRateAsync ile aynı filtre (IsActive), yıl filtresi yok (tüm yıllar);
    // yıl-fallback sıralaması (Year==hedef?1:0 desc, Year desc) çağıran tarafça bellekte uygulanır.
    Task<List<(int TenantCategoryId, int ChargeTypeId, int Year, RateValueDto Rate)>> GetRatesAsync(
        IReadOnlyCollection<int> tenantCategoryIds, IReadOnlyCollection<int> chargeTypeIds);
    Task<List<ParentRateRowDto>> GetRowsByYearAndCategoryAsync(int year, int? tenantCategoryId);
}

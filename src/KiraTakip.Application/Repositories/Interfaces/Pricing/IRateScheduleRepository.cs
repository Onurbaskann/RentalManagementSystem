using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.RateHierarchy;
using KiraTakip.Models.Dtos.RateSchedule;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Pricing;

public interface IRateScheduleRepository : IRepositoryBase<RateSchedule>
{
    Task<PagedResult<RateYearSummaryDto>> GetYearSummariesPagedAsync(TableQuery query);
    Task<RateValueDto?> GetRateAsync(int kategoriId, int chargeTypeId, int donemYil);
    Task<List<ParentRateRowDto>> GetRowsByYearAndCategoryAsync(int year, int? tenantCategoryId);
}

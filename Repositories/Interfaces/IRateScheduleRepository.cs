using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.RateSchedule;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IRateScheduleRepository : IRepositoryBase<RateSchedule>
{
    Task<PagedResult<RateYearSummaryDto>> GetYearSummariesPagedAsync(TableQuery query);
    Task<RateValueDto?> GetRateAsync(int kategoriId, int chargeTypeId, int donemYil);
    Task<List<ParentRateRowViewModel>> GetRowsByYearAndCategoryAsync(int year, int? tenantCategoryId);
}

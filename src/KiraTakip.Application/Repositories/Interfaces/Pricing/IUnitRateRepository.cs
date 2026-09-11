using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.RateHierarchy;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Pricing;

public interface IUnitRateRepository : IRepositoryBase<UnitRate>
{
    Task<RateValueDto?> GetRateAsync(int unitId, int tenantCategoryId, int chargeTypeId);
    Task<List<ParentRateRowDto>> GetRowsByUnitAsync(int unitId, int? tenantCategoryId);
    Task<UnitPricingContextDto> GetPricingContextAsync(int unitId, int year);
    Task<List<UnitRate>> GetForUpdateAsync(int unitId);
}

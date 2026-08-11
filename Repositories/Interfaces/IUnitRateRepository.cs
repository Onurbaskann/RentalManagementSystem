using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IUnitRateRepository : IRepositoryBase<UnitRate>
{
    Task<RateValueDto?> GetRateAsync(int unitId, int tenantCategoryId, int chargeTypeId);
    Task<List<ParentRateCardViewModel>> GetCardsByUnitAsync(int unitId, int? tenantCategoryId);
    Task<UnitPricingContextDto> GetPricingContextAsync(int unitId, int year);
    Task<List<UnitRate>> GetForUpdateAsync(int unitId);
}

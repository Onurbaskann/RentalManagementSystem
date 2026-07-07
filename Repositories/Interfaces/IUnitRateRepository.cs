using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IUnitRateRepository : IBaseRepository<UnitRate>
{
    Task<RateValueDto?> GetRateAsync(int unitId, int tenantCategoryId, int chargeTypeId);
    Task<List<ParentTarifeKartViewModel>> GetByBirimForKartAsync(int unitId, int? tenantCategoryId);
}

using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ITasinmazTarifeRepository : IBaseRepository<PropertyRateOverride>
{
    Task<List<PropertyRateOverride>> GetByPropertyIdAsync(int propertyId);
    Task<List<Category>> GetKiraciKategorileriAsync();
    Task<List<ChargeType>> GetBorcTipleriMatrisIcinAsync();
    Task<List<PropertyRateOverride>> GetForHiyerarsiAsync(int propertyId, int? kategoriId);
    Task<RateValueDto?> GetRateAsync(int propertyId, int kategoriId, int chargeTypeId);
}

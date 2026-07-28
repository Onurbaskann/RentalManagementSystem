using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IPropertyRateOverrideRepository : IBaseRepository<PropertyRateOverride>
{
    Task<List<PropertyRateOverride>> GetByPropertyIdAsync(int propertyId);
    Task<PropertyPricingContextDto> GetPricingContextAsync(int propertyId);
    Task<List<PropertyRateOverride>> GetForHiyerarsiAsync(int propertyId, int? kategoriId);
    Task<RateValueDto?> GetRateAsync(int propertyId, int kategoriId, int chargeTypeId);
}

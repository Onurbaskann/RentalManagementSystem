using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Property;

namespace KiraTakip.Repositories.Interfaces.Pricing;

public interface IPropertyRateOverrideRepository : IRepositoryBase<PropertyRateOverride>
{
    Task<List<PropertyRateOverride>> GetByPropertyIdAsync(int propertyId);
    Task<PropertyPricingContextDto> GetPricingContextAsync(int propertyId);
    Task<List<PropertyRateOverride>> GetForHiyerarsiAsync(int propertyId, int? kategoriId);
    Task<RateValueDto?> GetRateAsync(int propertyId, int kategoriId, int chargeTypeId);
    // Toplu (bulk) hali — GetRateAsync ile birebir aynı filtre (IsActive), tek taşınmazın tüm kategori/borç tipi kombinasyonları için.
    Task<List<(int TenantCategoryId, int ChargeTypeId, RateValueDto Rate)>> GetActiveRatesAsync(int propertyId);
}

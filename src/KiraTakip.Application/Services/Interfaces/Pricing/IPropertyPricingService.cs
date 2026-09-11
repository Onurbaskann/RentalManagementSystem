using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Property;

namespace KiraTakip.Services.Interfaces.Pricing;

public interface IPropertyPricingService
{
    Task<PropertyPricingMatrixDto> GetMatrixAsync(GetPropertyPricingMatrixInput input);
    Task SaveMatrixAsync(SavePropertyPricingMatrixInput input);
}

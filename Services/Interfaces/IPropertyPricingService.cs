using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IPropertyPricingService
{
    Task<PropertyPricingMatrixDto> GetMatrixAsync(GetPropertyPricingMatrixInput input);
    Task SaveMatrixAsync(SavePropertyPricingMatrixInput input);
}

using System.Threading.Tasks;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces.Pricing;

public interface IUnitPricingService
{
    Task<UnitPricingDataDto> GetPricingMatrixAsync(GetUnitPricingInput input);
    Task SavePricingMatrixAsync(SaveUnitPricingInput input);
}

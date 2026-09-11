using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.RateHierarchy;

namespace KiraTakip.Services.Interfaces.Pricing;

public interface IRateHierarchyService
{
    Task<ParentRateCardDto?> GetParentForAsync(GetParentRateInput input);
    Task<ParentReservationRateOverrideCardDto?> GetReservationParentAsync(
        GetParentReservationRateInput input);
}

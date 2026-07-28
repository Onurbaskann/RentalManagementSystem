using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IRateHierarchyService
{
    Task<ParentRateCardViewModel?> GetParentForAsync(GetParentRateInput input);
    Task<ParentReservationRateOverrideCardViewModel?> GetReservationParentAsync(
        GetParentReservationRateInput input);
}

using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IManualChargeService
{
    Task<List<ManualChargeListItemDto>> GetAllAsync(GetManualChargesInput input);
    Task<int> GetCancelledCountAsync(GetCancelledManualChargeCountInput input);
    Task CreateAsync(CreateManualChargeInput input);
    Task CancelAsync(CancelManualChargeInput input);
    Task<List<LeaseDropdownDto>> GetActiveLeasesAsync(GetActiveManualChargeLeasesInput input);
    Task<List<ChargeTypeLookupDto>> GetManualChargeTypesAsync();
    Task<List<UnitLookupDto>> GetAllUnitsAsync(GetManualChargeUnitsInput input);
}

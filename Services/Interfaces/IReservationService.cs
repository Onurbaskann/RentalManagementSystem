using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;

namespace KiraTakip.Services.Interfaces;

public interface IReservationService
{
    Task<List<ReservationListItemDto>> GetAllAsync(GetReservationsInput input);
    Task<PagedResult<ReservationListItemDto>> GetPageAsync(GetReservationsPageInput input);
    Task<List<ReservationListItemDto>> GetTenantReservationsAsync(GetTenantReservationsInput input);
    Task<PagedResult<ReservationListItemDto>> GetTenantReservationsPageAsync(GetTenantReservationsPageInput input);
    Task<ReservationListItemDto> GetByIdAsync(GetReservationByIdInput input);
    Task<ReservationCalculationResultDto> CalculateAsync(CalculateReservationInput input);
    Task<int> CreateAsync(CreateReservationInput input);
    Task CancelAsync(CancelReservationInput input);
    Task<int> TransferToChargeAsync(TransferReservationToChargeInput input);
    Task<ReservationFormOptionsDto> GetFormOptionsAsync(GetReservationFormOptionsInput input);

    // Ücret kuralları (birime özel)
    Task<List<ReservationRateOverrideListItemDto>> GetRateRulesAsync();
    Task<PagedResult<ReservationRateOverrideListItemDto>> GetRateRulesPagedAsync(TableQuery query);
    Task<ReservationRateOverride?> GetRateRuleByIdAsync(GetRateRuleByIdInput input);
    Task SaveRateRuleAsync(SaveReservationRateRuleInput input);
    Task SaveUnitReservationRateRuleAsync(SaveUnitReservationRateRuleInput input);
    Task ToggleRateRuleStatusAsync(ToggleRateRuleStatusInput input);
    Task ClearUnitReservationRateRuleAsync(ClearUnitReservationRateRuleInput input);
}

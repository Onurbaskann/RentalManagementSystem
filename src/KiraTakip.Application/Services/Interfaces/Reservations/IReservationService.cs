using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Services.Interfaces.Reservations;

public interface IReservationService
{
    Task<List<ReservationListItemDto>> GetAllAsync(GetReservationsInput input);
    Task<PagedResult<ReservationListItemDto>> GetPageAsync(GetReservationsPageInput input);
    Task<int> GetCancelledCountAsync(GetCancelledReservationCountInput input);
    Task<List<ReservationListItemDto>> GetTenantReservationsAsync(GetTenantReservationsInput input);
    Task<PagedResult<ReservationListItemDto>> GetTenantReservationsPageAsync(GetTenantReservationsPageInput input);
    Task<ReservationListItemDto> GetByIdAsync(GetReservationByIdInput input);
    Task<ReservationListItemDto> GetTenantByIdAsync(GetTenantReservationByIdInput input);
    Task<ReservationCalculationResultDto> CalculateAsync(CalculateReservationInput input);
    Task<int> CreateAsync(CreateReservationInput input);
    Task<int> CreateRequestAsync(CreateReservationRequestInput input);
    Task CancelAsync(CancelReservationInput input);
    Task CancelTenantAsync(CancelTenantReservationInput input);
    Task ApproveAsync(ApproveReservationInput input);
    Task RejectAsync(RejectReservationInput input);
    Task UpdateAsync(UpdateReservationInput input);
    Task<int> TransferToChargeAsync(TransferReservationToChargeInput input);
    Task<ReservationFormOptionsDto> GetFormOptionsAsync(GetReservationFormOptionsInput input);
    Task<ReservationCalendarResultDto> GetCalendarAsync(GetReservationCalendarInput input);
    Task<TenantReservationCalendarResultDto> GetTenantCalendarAsync(
        GetTenantReservationCalendarInput input);
    Task<ReservationAvailabilityResultDto> CheckAvailabilityAsync(
        CheckReservationAvailabilityInput input);

    // Ücret kuralları (alan bazlı)
    Task<List<ReservationSpaceOptionDto>> GetRateRuleSpaceOptionsAsync(SpaceAccessScope scope);
    Task<List<ChargeTypeLookupDto>> GetRateRuleChargeTypeOptionsAsync();
    Task<PagedResult<ReservationRateOverrideListItemDto>> GetRateRulesPagedAsync(TableQuery query, SpaceAccessScope scope);
    Task<ReservationRateOverride?> GetRateRuleByIdAsync(GetRateRuleByIdInput input);
    Task SaveRateRuleAsync(SaveReservationRateRuleInput input);
    Task ToggleRateRuleStatusAsync(ToggleRateRuleStatusInput input);
}

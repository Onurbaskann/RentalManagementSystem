using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IChargeRepository : IBaseRepository<Charge>
{
    // Okuma — DTO döner
    Task<List<ChargeListItemDto>> GetListAsync(int? leaseId, List<int>? authorizedPropertyIds, List<int>? authorizedUnitIds = null);
    Task<PagedResult<ChargeListItemDto>> GetPagedListAsync(TableQuery q, int? leaseId, List<int>? authorizedPropertyIds, List<int>? authorizedUnitIds = null);
    Task<PagedResult<ChargeListItemDto>> GetTenantPagedListAsync(GetTenantChargeIndexInput input);
    Task<ChargeDetailDto?> GetDetailsAsync(int id);
    Task<ChargeDetailDto?> GetTenantDetailsAsync(int chargeId, int tenantId, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<ChargeIndexOptionsDto> GetIndexOptionsAsync(GetChargeIndexOptionsInput input);
    Task<CurrentLeaseChargeDto> GetCurrentLeaseChargeAsync(GetCurrentLeaseChargeInput input);
    Task<TenantLeaseChargeDataDto> GetTenantLeaseDataAsync(GetTenantLeaseChargeDataInput input);
    Task<ManualLeaseChargeSummaryDto> GetManualLeaseChargeSummaryAsync(GetManualLeaseChargeSummaryInput input);
    Task<TenantChargeOverviewDto> GetTenantChargeOverviewAsync(int tenantId, DateTime today, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<TenantPanelChargeDataDto> GetTenantPanelDataAsync(GetTenantPanelChargeDataInput input);
    Task<MonthlyCollectionReportDto> GetMonthlyCollectionReportAsync(
        GetMonthlyCollectionReportInput input);
    Task<List<PaymentPortalChargeDto>> GetPaymentPortalChargesAsync(
        int tenantId,
        DateTime dueDateLimit,
        CancellationToken cancellationToken = default);

    // Manuel Borç — DTO döner
    Task<List<ManualChargeListItemDto>> GetManualChargeListAsync(
        List<int>? propertyIds,
        string? status = null,
        string? relation = null,
        int? leaseId = null,
        List<int>? unitIds = null);
    Task<int> GetCancelledManualChargeCountAsync(
        List<int>? propertyIds,
        List<int>? unitIds = null);

    // Business logic — entity döner (tracked)
    Task<List<Charge>> GetChargesToMarkOverdueAsync(DateTime today);
    Task<Charge?> GetManualChargeByIdAsync(
        int id,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<List<Charge>> GetPendingReminderChargesAsync(
        GetPendingChargeRemindersInput input,
        CancellationToken cancellationToken);

    // Hesaplama
    Task<bool> HasActiveForUnitTypeAsync(int unitTypeId);
    Task<Charge?> GetByReservationWithAllocationsAsync(int reservationId);
    Task<bool> ExistsForReservationAsync(int reservationId);

    // Üretim yardımcıları (ChargeGenerationService için)
    Task<List<Charge>> GetSilineceklerAsync(int leaseId, DateTime ilkGun);
    Task DeleteRangeAsync(IEnumerable<Charge> entities);
}

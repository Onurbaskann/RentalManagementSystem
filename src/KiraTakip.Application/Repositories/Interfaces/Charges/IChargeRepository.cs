using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Charge;
using KiraTakip.Models.Dtos.TenantPanel;
using KiraTakip.Models.Dtos.Report;
using KiraTakip.Models.Dtos.ChargeReminder;

namespace KiraTakip.Repositories.Interfaces.Charges;

public interface IChargeRepository : IRepositoryBase<Charge>
{
    // Okuma — DTO döner
    Task<List<ChargeListItemDto>> GetListAsync(int? leaseId, SpaceAccessScope? scope);
    Task<PagedResult<ChargeListItemDto>> GetPagedListAsync(TableQuery q, int? leaseId, SpaceAccessScope? scope, int? spaceId = null);
    Task<PagedResult<ChargeListItemDto>> GetTenantPagedListAsync(GetTenantChargeIndexInput input);
    Task<ChargeDetailDto?> GetDetailsAsync(int id);
    Task<ChargeDetailDto?> GetTenantDetailsAsync(int chargeId, int tenantId, SpaceAccessScope? scope = null);
    Task<ChargeIndexOptionsDto> GetIndexOptionsAsync(GetChargeIndexOptionsInput input);
    Task<CurrentLeaseChargeDto> GetCurrentLeaseChargeAsync(GetCurrentLeaseChargeInput input);
    Task<TenantLeaseChargeDataDto> GetTenantLeaseDataAsync(GetTenantLeaseChargeDataInput input);
    Task<ManualLeaseChargeSummaryDto> GetManualLeaseChargeSummaryAsync(GetManualLeaseChargeSummaryInput input);
    Task<TenantChargeOverviewDto> GetTenantChargeOverviewAsync(int tenantId, DateTime today, SpaceAccessScope? scope = null);
    Task<TenantPanelChargeDataDto> GetTenantPanelDataAsync(GetTenantPanelChargeDataInput input);
    Task<MonthlyCollectionReportDto> GetMonthlyCollectionReportAsync(
        GetMonthlyCollectionReportInput input);
    Task<PriorYearsOverdueSummary> GetPriorYearsOverdueAsync(
        GetMonthlyCollectionReportInput input);
    // Manuel Borç — DTO döner
    Task<List<ManualChargeListItemDto>> GetManualChargeListAsync(
        SpaceAccessScope? scope,
        string? status = null,
        string? relation = null,
        int? leaseId = null);
    Task<PagedResult<ManualChargeListItemDto>> GetManualChargePagedListAsync(
        TableQuery query,
        SpaceAccessScope? scope,
        string? relation = null,
        int? leaseId = null);
    Task<int> GetCancelledManualChargeCountAsync(SpaceAccessScope? scope);
    // Düz liste: aktif alanlar (kapsamdakiler), ad zinciriyle.
    Task<List<ChargeSpaceOptionDto>> GetManualChargeSpaceOptionsAsync(SpaceAccessScope? scope);

    // Business logic — entity döner (tracked)
    Task<List<Charge>> GetChargesToMarkOverdueAsync(DateTime today);
    Task<Charge?> GetManualChargeByIdAsync(int id, SpaceAccessScope? scope);
    Task<List<Charge>> GetPendingReminderChargesAsync(
        GetPendingChargeRemindersInput input,
        CancellationToken cancellationToken);

    // Hesaplama
    Task<Charge?> GetByReservationWithAllocationsAsync(int reservationId);
    Task<bool> ExistsForReservationAsync(int reservationId);

    // Üretim yardımcıları (ChargeGenerationService için)
    Task<List<Charge>> GetSilineceklerAsync(int leaseId, DateTime ilkGun);
    Task DeleteRangeAsync(IEnumerable<Charge> entities);
    Task<List<string>> GetExistingChargeNosAsync();
}

using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IChargeService
{
    // Listeleme — DTO döner
    Task<List<ChargeListItemDto>> GetListAsync(GetChargesInput input);
    Task<PagedResult<ChargeListItemDto>> GetPagedAsync(GetChargesPageInput input);
    Task<ChargeDetailDto?> GetDetailsAsync(GetChargeDetailsInput input);
    Task<ChargeDetailDto> GetTenantDetailsAsync(GetTenantChargeDetailsInput input);
    Task<ChargeIndexOptionsDto> GetIndexOptionsAsync(GetChargeIndexOptionsInput input);
    Task<CurrentLeaseChargeDto> GetCurrentLeaseChargeAsync(GetCurrentLeaseChargeInput input);
    Task<TenantLeaseChargeDataDto> GetTenantLeaseDataAsync(GetTenantLeaseChargeDataInput input);
    Task<ManualLeaseChargeSummaryDto> GetManualLeaseChargeSummaryAsync(GetManualLeaseChargeSummaryInput input);
    Task<TenantChargeIndexDataDto> GetTenantChargeIndexAsync(GetTenantChargeIndexInput input);
    Task<MonthlyCollectionReportDto> GetMonthlyCollectionReportAsync(
        GetMonthlyCollectionReportInput input);

    // Business operations
    Task UpdateDelaysAsync();
    Task UpdatePaidAmountAsync(UpdateChargePaidAmountInput input);
}

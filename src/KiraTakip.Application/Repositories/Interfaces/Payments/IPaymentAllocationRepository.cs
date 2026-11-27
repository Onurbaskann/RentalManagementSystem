using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.TenantPanel;
using KiraTakip.Models.Dtos.BankTransaction;
using KiraTakip.Models.Dtos.Document;
using KiraTakip.Models.Dtos.Report;

namespace KiraTakip.Repositories.Interfaces.Payments;

public interface IPaymentAllocationRepository : IRepositoryBase<PaymentAllocation>
{
    Task<List<PaymentListItemDto>> GetListAsync(
        int? chargeId,
        SpaceAccessScope? scope);
    Task<PagedResult<PaymentListItemDto>> GetPagedListAsync(
        TableQuery query,
        int? chargeId,
        SpaceAccessScope? scope);
    Task<PaymentDetailDto?> GetDetailsAsync(
        int id,
        SpaceAccessScope? scope);
    Task<PaymentAllocation?> GetForDecisionAsync(
        int id,
        SpaceAccessScope? scope);
    Task<decimal> GetPaidAmountAsync(int chargeId);
    Task<int?> GetChargeLineItemIdAsync(int paymentId);
    Task<decimal> GetTenantApprovedTotalAsync(int tenantId, SpaceAccessScope? scope = null);
    Task<decimal> GetPendingAmountAsync(int chargeId, int tenantId);
    Task<TenantPanelPaymentDataDto> GetTenantPanelDataAsync(GetTenantPanelPaymentDataInput input);
    Task<PaymentMatchingContextDto?> GetMatchingContextAsync(int paymentId);
    Task<PaymentMatchingBasisDto?> GetMatchingBasisAsync(int paymentId);
    Task<List<PaymentCandidateDto>> GetCandidatesAsync(
        PaymentMatchingBasisDto basis,
        PaymentMatchingPolicyDto policy,
        SpaceAccessScope? scope);
    Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int paymentId);
    Task<List<string>> GetExistingPaymentNosAsync();
    Task<List<MonthlyCollectionReportRowDto>> GetCollectedByMonthAsync(GetMonthlyCollectionReportInput input);
    Task<List<ReportYearCountDto>> GetPaymentYearBreakdownForChargeYearAsync(GetMonthlyCollectionReportInput input);
    Task<List<ReportYearAmountDto>> GetCashBasisByChargeYearAsync(GetMonthlyCollectionReportInput input);
}

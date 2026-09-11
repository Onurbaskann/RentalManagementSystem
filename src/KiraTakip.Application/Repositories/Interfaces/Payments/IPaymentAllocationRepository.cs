using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.TenantPanel;
using KiraTakip.Models.Dtos.BankTransaction;
using KiraTakip.Models.Dtos.Document;

namespace KiraTakip.Repositories.Interfaces.Payments;

public interface IPaymentAllocationRepository : IRepositoryBase<PaymentAllocation>
{
    Task<List<PaymentListItemDto>> GetListAsync(
        int? chargeId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PagedResult<PaymentListItemDto>> GetPagedListAsync(
        TableQuery query,
        int? chargeId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PaymentDetailDto?> GetDetailsAsync(
        int id,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PaymentAllocation?> GetForDecisionAsync(
        int id,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<decimal> GetPaidAmountAsync(int chargeId);
    Task<int?> GetChargeLineItemIdAsync(int paymentId);
    Task<decimal> GetTenantApprovedTotalAsync(int tenantId, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<decimal> GetPendingAmountAsync(int chargeId, int tenantId);
    Task<TenantPanelPaymentDataDto> GetTenantPanelDataAsync(GetTenantPanelPaymentDataInput input);
    Task<PaymentMatchingContextDto?> GetMatchingContextAsync(int paymentId);
    Task<PaymentMatchingBasisDto?> GetMatchingBasisAsync(int paymentId);
    Task<List<PaymentCandidateDto>> GetCandidatesAsync(
        PaymentMatchingBasisDto basis,
        PaymentMatchingPolicyDto policy,
        IReadOnlyList<int>? propertyIds,
        IReadOnlyList<int>? unitIds = null);
    Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int paymentId);
}

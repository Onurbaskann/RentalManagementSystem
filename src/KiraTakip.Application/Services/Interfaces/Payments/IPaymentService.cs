using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Payment;

namespace KiraTakip.Services.Interfaces.Payments;

public interface IPaymentService
{
    Task<List<PaymentListItemDto>> GetAllAsync(GetPaymentsInput input);
    Task<PagedResult<PaymentListItemDto>> GetPagedAsync(GetPagedPaymentsInput input);
    Task<PaymentDetailDto?> GetByIdAsync(GetPaymentByIdInput input);
    Task<ChargeDetailDto> GetCreationContextAsync(GetPaymentCreationContextInput input);
    Task<List<ChargeLineItemPaymentBalanceDto>> GetPayableLineItemsAsync(int chargeId);
    Task<int> CreateAsync(CreatePaymentInput input);
    Task ReportTenantPaymentAsync(ReportTenantPaymentInput input);
    Task ApproveAsync(ApprovePaymentInput input);
    Task RejectAsync(RejectPaymentInput input);
}

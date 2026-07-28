using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IPaymentService
{
    Task<List<PaymentListItemDto>> GetAllAsync(GetPaymentsInput input);
    Task<PagedResult<PaymentListItemDto>> GetPagedAsync(GetPagedPaymentsInput input);
    Task<PaymentDetailDto?> GetByIdAsync(GetPaymentByIdInput input);
    Task<ChargeDetailDto> GetCreationContextAsync(GetPaymentCreationContextInput input);
    Task<int> CreateAsync(CreatePaymentInput input);
    Task ReportTenantPaymentAsync(ReportTenantPaymentInput input);
    Task ApproveAsync(ApprovePaymentInput input);
    Task RejectAsync(RejectPaymentInput input);
}

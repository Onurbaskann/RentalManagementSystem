using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IPaymentPortalService
{
    Task<PaymentPortalResultDto> GetAsync(
        GetPaymentPortalInput input,
        CancellationToken cancellationToken = default);
}

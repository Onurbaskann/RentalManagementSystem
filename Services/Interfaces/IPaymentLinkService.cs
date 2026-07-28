using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IPaymentLinkService
{
    Task<string> CreateAsync(
        CreatePaymentLinkInput input,
        CancellationToken cancellationToken = default);
    Task<PaymentLinkValidationResultDto> ValidateAsync(
        ValidatePaymentLinkInput input,
        CancellationToken cancellationToken = default);
    Task CancelAsync(
        CancelPaymentLinkInput input,
        CancellationToken cancellationToken = default);
}

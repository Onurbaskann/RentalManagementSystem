using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.PaymentStoreRouting;

namespace KiraTakip.Services.Interfaces.Payments;

public interface IPaymentStoreRoutingBusinessRules : IBusinessRules
{
    Task EnsureUpsertAllowedAsync(UpsertPaymentStoreRoutingInput input);
    Task<PaymentStoreRouting> GetActiveOverrideAsync(int id);
}

using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IPaymentBusinessRules : IBusinessRules
{
    void EnsureLineItemBelongsToCharge(ChargeLineItemPaymentBalanceDto balance, int chargeId);
    void EnsureLineItemPayable(ChargeLineItemPaymentBalanceDto balance);
    void EnsureAdminAmountWithinAvailable(ChargeLineItemPaymentBalanceDto balance, decimal amount);
    void EnsureTenantAmountWithinAvailable(ChargeLineItemPaymentBalanceDto balance, decimal amount);
    void EnsureApprovalWithinRemaining(ChargeLineItemPaymentBalanceDto balance, decimal amount);
    ChargeLineItemPaymentBalanceDto ResolveAutoSelectedLineItem(
        IReadOnlyList<ChargeLineItemPaymentBalanceDto> payableLineItems);
}

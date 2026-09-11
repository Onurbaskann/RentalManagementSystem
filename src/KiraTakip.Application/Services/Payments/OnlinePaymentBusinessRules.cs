using KiraTakip.Domain.Payments;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Models.Dtos.Payment;

namespace KiraTakip.Services.Payments;

public class OnlinePaymentBusinessRules : IOnlinePaymentBusinessRules
{
    public void EnsureAmountWithinAvailable(ChargeLineItemPaymentBalanceDto balance, decimal amount)
    {
        Guard.InvalidField(
            !PaymentAmountPolicy.IsPositive(amount),
            "Amount",
            "Tutar 0'dan büyük olmalıdır.",
            "ONLINE_PAYMENT_AMOUNT_NOT_POSITIVE");
        Guard.InvalidField(
            !PaymentAmountPolicy.DoesNotExceedAvailable(amount, balance.AvailableAmount),
            "Amount",
            $"Tutar, kalemin kullanılabilir kalan tutarından ({balance.AvailableAmount:N2} ₺) küçük veya eşit olmalıdır.",
            "ONLINE_PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE");
    }

    public bool IsValidStatusTransition(OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to)
        => OnlinePaymentStatusTransitionPolicy.CanTransition(from, to);

    public void EnsureValidStatusTransition(OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to)
    {
        Guard.Conflict(
            !IsValidStatusTransition(from, to),
            $"Sanal POS işlemi '{from}' durumundan '{to}' durumuna geçemez.",
            "ONLINE_PAYMENT_INVALID_STATUS_TRANSITION");
    }
}

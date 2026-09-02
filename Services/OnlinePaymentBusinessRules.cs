using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class OnlinePaymentBusinessRules : IOnlinePaymentBusinessRules
{
    private static readonly IReadOnlyDictionary<OnlinePaymentTransactionStatus, OnlinePaymentTransactionStatus[]> AllowedTransitions =
        new Dictionary<OnlinePaymentTransactionStatus, OnlinePaymentTransactionStatus[]>
        {
            [OnlinePaymentTransactionStatus.Pending] =
            [
                OnlinePaymentTransactionStatus.Approved,
                OnlinePaymentTransactionStatus.Failed,
                OnlinePaymentTransactionStatus.Cancelled,
                OnlinePaymentTransactionStatus.Unknown
            ],
            [OnlinePaymentTransactionStatus.Unknown] =
            [
                OnlinePaymentTransactionStatus.Approved,
                OnlinePaymentTransactionStatus.Failed,
                OnlinePaymentTransactionStatus.Cancelled
            ]
        };

    public void EnsureAmountWithinAvailable(ChargeLineItemPaymentBalanceDto balance, decimal amount)
    {
        Guard.InvalidField(
            amount <= 0,
            "Amount",
            "Tutar 0'dan büyük olmalıdır.",
            "ONLINE_PAYMENT_AMOUNT_NOT_POSITIVE");
        Guard.InvalidField(
            amount > balance.AvailableAmount,
            "Amount",
            $"Tutar, kalemin kullanılabilir kalan tutarından ({balance.AvailableAmount:N2} ₺) küçük veya eşit olmalıdır.",
            "ONLINE_PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE");
    }

    public bool IsValidStatusTransition(OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to)
        => AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public void EnsureValidStatusTransition(OnlinePaymentTransactionStatus from, OnlinePaymentTransactionStatus to)
    {
        Guard.Conflict(
            !IsValidStatusTransition(from, to),
            $"Sanal POS işlemi '{from}' durumundan '{to}' durumuna geçemez.",
            "ONLINE_PAYMENT_INVALID_STATUS_TRANSITION");
    }
}

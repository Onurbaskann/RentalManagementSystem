using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Payments;

public static class OnlinePaymentStatusTransitionPolicy
{
    public static OnlinePaymentTransactionStatus ResolveInitialStatus(bool isSessionSuccessful)
        => isSessionSuccessful
            ? OnlinePaymentTransactionStatus.Pending
            : OnlinePaymentTransactionStatus.Failed;

    public static bool CanTransition(
        OnlinePaymentTransactionStatus from,
        OnlinePaymentTransactionStatus to)
        => from switch
        {
            OnlinePaymentTransactionStatus.Pending =>
                to is OnlinePaymentTransactionStatus.Approved
                    or OnlinePaymentTransactionStatus.Failed
                    or OnlinePaymentTransactionStatus.Cancelled
                    or OnlinePaymentTransactionStatus.Unknown,
            OnlinePaymentTransactionStatus.Unknown =>
                to is OnlinePaymentTransactionStatus.Approved
                    or OnlinePaymentTransactionStatus.Failed
                    or OnlinePaymentTransactionStatus.Cancelled,
            _ => false
        };
}

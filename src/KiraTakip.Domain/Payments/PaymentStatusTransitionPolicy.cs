using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Payments;

public static class PaymentStatusTransitionPolicy
{
    public static bool CanTransition(PaymentStatus from, PaymentStatus to)
        => from switch
        {
            PaymentStatus.PendingApproval =>
                to is PaymentStatus.Approved or PaymentStatus.Rejected,
            _ => false
        };
}

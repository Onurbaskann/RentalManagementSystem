using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Payments;

public enum ChargePaymentEligibility
{
    Payable = 0,
    Cancelled = 1,
    NoRemainingBalance = 2
}

public static class PaymentChargeEligibilityPolicy
{
    public static ChargePaymentEligibility CheckEligibility(
        ChargeStatus status,
        decimal totalAmount,
        decimal paidAmount)
    {
        if (status == ChargeStatus.Cancelled)
            return ChargePaymentEligibility.Cancelled;

        if (status == ChargeStatus.Paid || totalAmount - paidAmount <= 0)
            return ChargePaymentEligibility.NoRemainingBalance;

        return ChargePaymentEligibility.Payable;
    }
}

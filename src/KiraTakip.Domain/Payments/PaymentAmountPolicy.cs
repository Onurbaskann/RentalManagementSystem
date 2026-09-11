namespace KiraTakip.Domain.Payments;

public static class PaymentAmountPolicy
{
    public static bool BelongsToCharge(int lineItemChargeId, int chargeId)
        => lineItemChargeId == chargeId;

    public static bool HasRemainingBalance(decimal remainingAmount)
        => remainingAmount > 0;

    public static bool HasAvailableBalance(decimal availableAmount)
        => availableAmount > 0;

    public static bool IsPositive(decimal amount)
        => amount > 0;

    public static bool DoesNotExceedAvailable(decimal amount, decimal availableAmount)
        => amount <= availableAmount;

    public static bool CanApprove(decimal totalAmount, decimal approvedAmount, decimal amount)
        => approvedAmount + amount <= totalAmount;
}

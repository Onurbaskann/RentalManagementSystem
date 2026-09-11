using KiraTakip.Domain.Financial;
using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Charges;

public enum ManualChargeLeaseValidationResult
{
    Valid = 0,
    Terminated = 1,
    NotActive = 2,
    TenantMismatch = 3,
    UnitMismatch = 4
}

public enum ManualChargeCancellationValidationResult
{
    Valid = 0,
    AlreadyCancelled = 1,
    HasApprovedPayment = 2
}

public readonly record struct ManualChargeAmountCalculation(
    decimal Amount,
    decimal VatRate,
    decimal VatAmount,
    decimal TotalAmount);

public static class ManualChargePolicy
{
    public static ManualChargeLeaseValidationResult ValidateLease(
        LeaseStatus leaseStatus,
        int leaseTenantId,
        int targetTenantId,
        int leaseUnitId,
        int targetUnitId)
    {
        if (leaseStatus == LeaseStatus.Terminated)
            return ManualChargeLeaseValidationResult.Terminated;

        if (leaseStatus != LeaseStatus.Active)
            return ManualChargeLeaseValidationResult.NotActive;

        if (leaseTenantId != targetTenantId)
            return ManualChargeLeaseValidationResult.TenantMismatch;

        if (leaseUnitId != targetUnitId)
            return ManualChargeLeaseValidationResult.UnitMismatch;

        return ManualChargeLeaseValidationResult.Valid;
    }

    public static ManualChargeAmountCalculation CalculateAmounts(
        decimal amount,
        bool isVatApplied,
        decimal vatRate)
    {
        var effectiveVatRate = isVatApplied ? vatRate : 0m;
        var vatAmount = isVatApplied
            ? VatPolicy.CalculateAmount(amount, vatRate)
            : 0m;
        var totalAmount = isVatApplied
            ? VatPolicy.CalculateIncludedAmount(amount, vatRate)
            : amount;

        return new ManualChargeAmountCalculation(
            amount,
            effectiveVatRate,
            vatAmount,
            totalAmount);
    }

    public static ManualChargeCancellationValidationResult ValidateCancellation(
        ChargeStatus status,
        bool hasApprovedPayment)
    {
        if (status == ChargeStatus.Cancelled)
            return ManualChargeCancellationValidationResult.AlreadyCancelled;

        if (hasApprovedPayment)
            return ManualChargeCancellationValidationResult.HasApprovedPayment;

        return ManualChargeCancellationValidationResult.Valid;
    }

    public static bool HasApprovedPayment(IEnumerable<PaymentStatus> paymentStatuses)
    {
        foreach (var status in paymentStatuses)
        {
            if (status == PaymentStatus.Approved)
                return true;
        }

        return false;
    }
}

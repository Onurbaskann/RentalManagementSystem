using KiraTakip.Domain.Charges;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class ManualChargePolicyTests
{
    [Fact]
    public void ValidateLease_WhenActiveAndMatching_ShouldReturnValid()
    {
        var result = ManualChargePolicy.ValidateLease(
            LeaseStatus.Active,
            leaseTenantId: 10,
            targetTenantId: 10,
            leaseUnitId: 20,
            targetUnitId: 20);

        Assert.Equal(ManualChargeLeaseValidationResult.Valid, result);
    }

    [Fact]
    public void ValidateLease_WhenTerminated_ShouldReturnTerminated()
    {
        var result = ManualChargePolicy.ValidateLease(
            LeaseStatus.Terminated,
            leaseTenantId: 10,
            targetTenantId: 10,
            leaseUnitId: 20,
            targetUnitId: 20);

        Assert.Equal(ManualChargeLeaseValidationResult.Terminated, result);
    }

    [Theory]
    [InlineData(LeaseStatus.Ended)]
    [InlineData(LeaseStatus.Draft)]
    [InlineData(LeaseStatus.RevisionRequested)]
    public void ValidateLease_WhenOtherInactiveStatus_ShouldReturnNotActive(LeaseStatus status)
    {
        var result = ManualChargePolicy.ValidateLease(
            status,
            leaseTenantId: 10,
            targetTenantId: 10,
            leaseUnitId: 20,
            targetUnitId: 20);

        Assert.Equal(ManualChargeLeaseValidationResult.NotActive, result);
    }

    [Fact]
    public void ValidateLease_WhenTenantMismatch_ShouldReturnTenantMismatch()
    {
        var result = ManualChargePolicy.ValidateLease(
            LeaseStatus.Active,
            leaseTenantId: 10,
            targetTenantId: 11,
            leaseUnitId: 20,
            targetUnitId: 20);

        Assert.Equal(ManualChargeLeaseValidationResult.TenantMismatch, result);
    }

    [Fact]
    public void ValidateLease_WhenUnitMismatch_ShouldReturnUnitMismatch()
    {
        var result = ManualChargePolicy.ValidateLease(
            LeaseStatus.Active,
            leaseTenantId: 10,
            targetTenantId: 10,
            leaseUnitId: 20,
            targetUnitId: 21);

        Assert.Equal(ManualChargeLeaseValidationResult.UnitMismatch, result);
    }

    [Fact]
    public void CalculateAmounts_WhenVatApplied_ShouldRoundVatToTwoDecimalsAndPreserveAmount()
    {
        // 123.456m * 20 / 100 = 24.6912m => round 24.69m
        var result = ManualChargePolicy.CalculateAmounts(123.456m, isVatApplied: true, vatRate: 20m);

        Assert.Equal(123.456m, result.Amount);
        Assert.Equal(20m, result.VatRate);
        Assert.Equal(24.69m, result.VatAmount);
        Assert.Equal(148.146m, result.TotalAmount);
    }

    [Fact]
    public void CalculateAmounts_WhenVatNotApplied_ShouldReturnZeroVat()
    {
        var result = ManualChargePolicy.CalculateAmounts(500.50m, isVatApplied: false, vatRate: 20m);

        Assert.Equal(500.50m, result.Amount);
        Assert.Equal(0m, result.VatRate);
        Assert.Equal(0m, result.VatAmount);
        Assert.Equal(500.50m, result.TotalAmount);
    }

    [Fact]
    public void CalculateAmounts_ShouldNotRoundOriginalAmount()
    {
        var result = ManualChargePolicy.CalculateAmounts(999.9999m, isVatApplied: false, vatRate: 0m);

        Assert.Equal(999.9999m, result.Amount);
        Assert.Equal(999.9999m, result.TotalAmount);
    }

    [Fact]
    public void ValidateCancellation_WhenAlreadyCancelled_ShouldReturnAlreadyCancelled()
    {
        var result = ManualChargePolicy.ValidateCancellation(
            ChargeStatus.Cancelled,
            hasApprovedPayment: false);

        Assert.Equal(ManualChargeCancellationValidationResult.AlreadyCancelled, result);
    }

    [Fact]
    public void ValidateCancellation_WhenHasApprovedPayment_ShouldReturnHasApprovedPayment()
    {
        var result = ManualChargePolicy.ValidateCancellation(
            ChargeStatus.Pending,
            hasApprovedPayment: true);

        Assert.Equal(ManualChargeCancellationValidationResult.HasApprovedPayment, result);
    }

    [Theory]
    [InlineData(ChargeStatus.Pending)]
    [InlineData(ChargeStatus.PartiallyPaid)]
    [InlineData(ChargeStatus.Overdue)]
    public void ValidateCancellation_WhenNotCancelledAndNoApprovedPayment_ShouldReturnValid(ChargeStatus status)
    {
        var result = ManualChargePolicy.ValidateCancellation(
            status,
            hasApprovedPayment: false);

        Assert.Equal(ManualChargeCancellationValidationResult.Valid, result);
    }

    [Fact]
    public void HasApprovedPayment_WhenContainsApproved_ShouldReturnTrue()
    {
        var statuses = new[] { PaymentStatus.PendingApproval, PaymentStatus.Approved, PaymentStatus.Rejected };
        var result = ManualChargePolicy.HasApprovedPayment(statuses);

        Assert.True(result);
    }

    [Fact]
    public void HasApprovedPayment_WhenNoApproved_ShouldReturnFalse()
    {
        var statuses = new[] { PaymentStatus.PendingApproval, PaymentStatus.Rejected };
        var result = ManualChargePolicy.HasApprovedPayment(statuses);

        Assert.False(result);
    }
}

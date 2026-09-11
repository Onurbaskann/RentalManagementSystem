using KiraTakip.Domain.Charges;
using KiraTakip.Domain.Payments;
using KiraTakip.Models.Enums;
using Xunit;

namespace KiraTakip.Tests;

public class PaymentChargeEligibilityPolicyTests
{
    [Fact]
    public void CheckEligibility_WhenChargeIsCancelled_ReturnsCancelled()
    {
        var result = PaymentChargeEligibilityPolicy.CheckEligibility(
            ChargeStatus.Cancelled,
            totalAmount: 1000m,
            paidAmount: 200m);

        Assert.Equal(ChargePaymentEligibility.Cancelled, result);
    }

    [Fact]
    public void CheckEligibility_WhenChargeIsCancelledEvenWithZeroRemaining_ReturnsCancelledFirst()
    {
        var result = PaymentChargeEligibilityPolicy.CheckEligibility(
            ChargeStatus.Cancelled,
            totalAmount: 1000m,
            paidAmount: 1000m);

        Assert.Equal(ChargePaymentEligibility.Cancelled, result);
    }

    [Fact]
    public void CheckEligibility_WhenChargeStatusIsPaid_ReturnsNoRemainingBalance()
    {
        var result = PaymentChargeEligibilityPolicy.CheckEligibility(
            ChargeStatus.Paid,
            totalAmount: 1000m,
            paidAmount: 500m);

        Assert.Equal(ChargePaymentEligibility.NoRemainingBalance, result);
    }

    [Theory]
    [InlineData(1000, 1000)]
    [InlineData(1000, 1200)]
    public void CheckEligibility_WhenRemainingAmountIsZeroOrNegative_ReturnsNoRemainingBalance(decimal total, decimal paid)
    {
        var result = PaymentChargeEligibilityPolicy.CheckEligibility(
            ChargeStatus.Pending,
            totalAmount: total,
            paidAmount: paid);

        Assert.Equal(ChargePaymentEligibility.NoRemainingBalance, result);
    }

    [Theory]
    [InlineData(ChargeStatus.Pending, 1000, 0)]
    [InlineData(ChargeStatus.Pending, 1000, 300)]
    [InlineData(ChargeStatus.PartiallyPaid, 1000, 999.99)]
    [InlineData(ChargeStatus.Overdue, 1500, 500)]
    public void CheckEligibility_WhenRemainingAmountIsPositiveAndNotCancelledOrPaid_ReturnsPayable(
        ChargeStatus status,
        decimal total,
        decimal paid)
    {
        var result = PaymentChargeEligibilityPolicy.CheckEligibility(
            status,
            totalAmount: total,
            paidAmount: paid);

        Assert.Equal(ChargePaymentEligibility.Payable, result);
    }
}

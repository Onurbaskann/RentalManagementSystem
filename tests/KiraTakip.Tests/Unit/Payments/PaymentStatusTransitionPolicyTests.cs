using KiraTakip.Domain.Payments;
using KiraTakip.Models.Enums;
using Xunit;

namespace KiraTakip.Tests;

public class PaymentStatusTransitionPolicyTests
{
    [Theory]
    [InlineData(PaymentStatus.PendingApproval, PaymentStatus.Approved, true)]
    [InlineData(PaymentStatus.PendingApproval, PaymentStatus.Rejected, true)]
    [InlineData(PaymentStatus.PendingApproval, PaymentStatus.PendingApproval, false)]
    public void CanTransition_FromPendingApproval_ValidatesCorrectly(PaymentStatus from, PaymentStatus to, bool expected)
    {
        var result = PaymentStatusTransitionPolicy.CanTransition(from, to);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PaymentStatus.Approved, PaymentStatus.PendingApproval)]
    [InlineData(PaymentStatus.Approved, PaymentStatus.Rejected)]
    [InlineData(PaymentStatus.Approved, PaymentStatus.Approved)]
    [InlineData(PaymentStatus.Rejected, PaymentStatus.PendingApproval)]
    [InlineData(PaymentStatus.Rejected, PaymentStatus.Approved)]
    [InlineData(PaymentStatus.Rejected, PaymentStatus.Rejected)]
    public void CanTransition_FromTerminalStatuses_ReturnsFalse(PaymentStatus from, PaymentStatus to)
    {
        var result = PaymentStatusTransitionPolicy.CanTransition(from, to);

        Assert.False(result);
    }
}

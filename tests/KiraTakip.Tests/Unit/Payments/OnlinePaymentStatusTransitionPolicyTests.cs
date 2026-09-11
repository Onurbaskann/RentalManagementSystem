using KiraTakip.Domain.Payments;
using KiraTakip.Models.Enums;
using Xunit;

namespace KiraTakip.Tests;

public class OnlinePaymentStatusTransitionPolicyTests
{
    [Theory]
    [InlineData(true, OnlinePaymentTransactionStatus.Pending)]
    [InlineData(false, OnlinePaymentTransactionStatus.Failed)]
    public void ResolveInitialStatus_ReturnsExpectedStatus(bool isSuccessful, OnlinePaymentTransactionStatus expected)
    {
        var result = OnlinePaymentStatusTransitionPolicy.ResolveInitialStatus(isSuccessful);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Approved, true)]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Failed, true)]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Cancelled, true)]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Unknown, true)]
    [InlineData(OnlinePaymentTransactionStatus.Pending, OnlinePaymentTransactionStatus.Pending, false)]
    public void CanTransition_FromPending_ValidatesCorrectly(
        OnlinePaymentTransactionStatus from,
        OnlinePaymentTransactionStatus to,
        bool expected)
    {
        var result = OnlinePaymentStatusTransitionPolicy.CanTransition(from, to);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(OnlinePaymentTransactionStatus.Unknown, OnlinePaymentTransactionStatus.Approved, true)]
    [InlineData(OnlinePaymentTransactionStatus.Unknown, OnlinePaymentTransactionStatus.Failed, true)]
    [InlineData(OnlinePaymentTransactionStatus.Unknown, OnlinePaymentTransactionStatus.Cancelled, true)]
    [InlineData(OnlinePaymentTransactionStatus.Unknown, OnlinePaymentTransactionStatus.Pending, false)]
    [InlineData(OnlinePaymentTransactionStatus.Unknown, OnlinePaymentTransactionStatus.Unknown, false)]
    public void CanTransition_FromUnknown_ValidatesCorrectly(
        OnlinePaymentTransactionStatus from,
        OnlinePaymentTransactionStatus to,
        bool expected)
    {
        var result = OnlinePaymentStatusTransitionPolicy.CanTransition(from, to);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(OnlinePaymentTransactionStatus.Approved)]
    [InlineData(OnlinePaymentTransactionStatus.Failed)]
    [InlineData(OnlinePaymentTransactionStatus.Cancelled)]
    public void CanTransition_FromTerminalStatuses_ReturnsFalseForAllTargets(OnlinePaymentTransactionStatus terminalStatus)
    {
        foreach (OnlinePaymentTransactionStatus target in System.Enum.GetValues(typeof(OnlinePaymentTransactionStatus)))
        {
            var result = OnlinePaymentStatusTransitionPolicy.CanTransition(terminalStatus, target);
            Assert.False(result);
        }
    }
}

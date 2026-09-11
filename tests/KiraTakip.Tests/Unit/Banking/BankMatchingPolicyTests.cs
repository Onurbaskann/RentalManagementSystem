using KiraTakip.Domain.Banking;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class BankMatchingPolicyTests
{
    [Theory]
    [InlineData(PaymentStatus.PendingApproval, true)]
    [InlineData(PaymentStatus.Approved, true)]
    [InlineData(PaymentStatus.Rejected, false)]
    public void CanMatchPayment_ShouldReturnExpected(PaymentStatus status, bool expected)
    {
        var result = BankMatchingPolicy.CanMatchPayment(status);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(BankMatchStatus.Unmatched, true)]
    [InlineData(BankMatchStatus.Matched, false)]
    [InlineData(BankMatchStatus.ManuallyMatched, false)]
    public void CanMatchTransaction_ShouldReturnExpected(BankMatchStatus status, bool expected)
    {
        var result = BankMatchingPolicy.CanMatchTransaction(status);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    [InlineData(null, null, true)]
    [InlineData(1, null, false)]
    [InlineData(null, 1, false)]
    public void AreStoreAccountsCompatible_ShouldReturnExpected(int? paymentAccountId, int? transactionAccountId, bool expected)
    {
        var result = BankMatchingPolicy.AreStoreAccountsCompatible(paymentAccountId, transactionAccountId);
        Assert.Equal(expected, result);
    }
}

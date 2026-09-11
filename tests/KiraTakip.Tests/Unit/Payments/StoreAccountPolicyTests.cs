using KiraTakip.Domain.Payments;

namespace KiraTakip.Tests;

public class StoreAccountPolicyTests
{
    [Theory]
    [InlineData("Paratika", true)]
    [InlineData("paratika", true)]
    [InlineData("Unknown", false)]
    public void IsProviderSupported_ShouldBeCaseInsensitive(
        string providerCode,
        bool expected)
        => Assert.Equal(expected, StoreAccountPolicy.IsProviderSupported(providerCode));

    [Theory]
    [InlineData("TRY", true)]
    [InlineData("try", true)]
    [InlineData("USD", false)]
    public void IsCurrencySupported_ShouldBeCaseInsensitive(
        string currency,
        bool expected)
        => Assert.Equal(expected, StoreAccountPolicy.IsCurrencySupported(currency));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void HasActiveAccount_ShouldReturnExpected(int count, bool expected)
        => Assert.Equal(expected, StoreAccountPolicy.HasActiveAccount(count));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void HasAccountConflict_ShouldReturnExpected(int count, bool expected)
        => Assert.Equal(expected, StoreAccountPolicy.HasAccountConflict(count));
}

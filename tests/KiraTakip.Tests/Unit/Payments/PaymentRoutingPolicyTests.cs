using KiraTakip.Domain.Payments;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class PaymentRoutingPolicyTests
{
    [Theory]
    [InlineData(PaymentRoutingScope.General, null, null, true)]
    [InlineData(PaymentRoutingScope.General, 1, null, false)]
    [InlineData(PaymentRoutingScope.Property, 1, null, true)]
    [InlineData(PaymentRoutingScope.Property, 1, 2, false)]
    [InlineData(PaymentRoutingScope.Unit, null, 2, true)]
    [InlineData(PaymentRoutingScope.Unit, 1, 2, false)]
    public void HasValidScopeSelection_ShouldMatchScopeWithIdentifiers(
        PaymentRoutingScope scope,
        int? propertyId,
        int? unitId,
        bool expected)
        => Assert.Equal(
            expected,
            PaymentRoutingPolicy.HasValidScopeSelection(scope, propertyId, unitId));

    [Fact]
    public void IsDefinedScope_ShouldRejectUnknownValue()
        => Assert.False(PaymentRoutingPolicy.IsDefinedScope((PaymentRoutingScope)999));

    [Theory]
    [InlineData(null, null, false)]
    [InlineData(1, null, true)]
    [InlineData(null, 2, true)]
    public void CanDeactivateOverride_ShouldRejectGeneralRouting(
        int? propertyId,
        int? unitId,
        bool expected)
        => Assert.Equal(
            expected,
            PaymentRoutingPolicy.CanDeactivateOverride(propertyId, unitId));
}

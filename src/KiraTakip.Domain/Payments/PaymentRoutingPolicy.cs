using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Payments;

public static class PaymentRoutingPolicy
{
    public static bool IsDefinedScope(PaymentRoutingScope scope)
        => Enum.IsDefined(scope);

    public static bool HasValidScopeSelection(
        PaymentRoutingScope scope,
        int? propertyId,
        int? unitId)
        => scope switch
        {
            PaymentRoutingScope.General => propertyId == null && unitId == null,
            PaymentRoutingScope.Property => propertyId > 0 && unitId == null,
            PaymentRoutingScope.Unit => propertyId == null && unitId > 0,
            _ => false
        };

    public static bool CanDeactivateOverride(int? propertyId, int? unitId)
        => propertyId.HasValue || unitId.HasValue;
}

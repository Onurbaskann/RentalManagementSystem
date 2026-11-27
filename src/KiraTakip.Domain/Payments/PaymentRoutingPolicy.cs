namespace KiraTakip.Domain.Payments;

public static class PaymentRoutingPolicy
{
    public static bool HasValidScopeSelection(int? spaceId)
        => spaceId == null || spaceId > 0;

    public static bool CanDeactivateOverride(int? spaceId)
        => spaceId.HasValue;
}

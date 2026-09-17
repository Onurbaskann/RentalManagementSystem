using KiraTakip.Models.Constants;

namespace KiraTakip.Domain.Leases;

public static class LeaseBillingPolicy
{
    public static bool ShouldIncludeChargeType(bool isRentFree, string chargeTypeCode)
        => !isRentFree
            || !string.Equals(
                chargeTypeCode,
                BorcTipiConsts.Kira,
                StringComparison.OrdinalIgnoreCase);

    public static bool HasChargeableAmount(decimal totalAmount)
        => totalAmount > 0;
}

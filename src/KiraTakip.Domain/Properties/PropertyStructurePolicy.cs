using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Properties;

public static class PropertyStructurePolicy
{
    public static bool SupportsUnitStructure(
        UnitStructure structure,
        bool supportsSingleUnit,
        bool supportsMultipleUnits)
        => structure == UnitStructure.SingleUnit
            ? supportsSingleUnit
            : supportsMultipleUnits;

    public static decimal CalculateSingleUnitArea(decimal closedArea, decimal openArea)
        => closedArea > 0 ? closedArea : openArea;

    public static bool IsValidNormalUnitUsage(UnitTypeUsage usage)
        => usage != UnitTypeUsage.Reservable;

    public static bool IsValidReservationUnitUsage(UnitTypeUsage usage)
        => usage == UnitTypeUsage.Reservable;

    public static bool RequiresChargeType(UnitTypeUsage usage)
        => usage == UnitTypeUsage.Reservable;

    public static int? ResolveChargeTypeId(UnitTypeUsage usage, int? chargeTypeId)
        => usage == UnitTypeUsage.Reservable ? chargeTypeId : null;
}

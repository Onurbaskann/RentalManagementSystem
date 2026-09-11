using KiraTakip.Domain.Properties;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

public class PropertyStructurePolicyTests
{
    [Theory]
    [InlineData(UnitStructure.SingleUnit, true, false, true)]
    [InlineData(UnitStructure.SingleUnit, false, true, false)]
    [InlineData(UnitStructure.MultipleUnits, true, false, false)]
    [InlineData(UnitStructure.MultipleUnits, false, true, true)]
    public void SupportsUnitStructure_ShouldEnforcePropertyTypeFlags(
        UnitStructure structure,
        bool supportsSingle,
        bool supportsMultiple,
        bool expected)
        => Assert.Equal(
            expected,
            PropertyStructurePolicy.SupportsUnitStructure(structure, supportsSingle, supportsMultiple));

    [Theory]
    [InlineData(100, 50, 100)]
    [InlineData(0, 50, 50)]
    [InlineData(-5, 50, 50)]
    public void CalculateSingleUnitArea_ShouldPreferClosedAreaWhenPositive(
        decimal closedArea,
        decimal openArea,
        decimal expected)
        => Assert.Equal(
            expected,
            PropertyStructurePolicy.CalculateSingleUnitArea(closedArea, openArea));

    [Theory]
    [InlineData(UnitTypeUsage.Rentable, true)]
    [InlineData(UnitTypeUsage.NonRentable, true)]
    [InlineData(UnitTypeUsage.Reservable, false)]
    public void IsValidNormalUnitUsage_ShouldRejectReservable(UnitTypeUsage usage, bool expected)
        => Assert.Equal(expected, PropertyStructurePolicy.IsValidNormalUnitUsage(usage));

    [Theory]
    [InlineData(UnitTypeUsage.Reservable, true)]
    [InlineData(UnitTypeUsage.Rentable, false)]
    [InlineData(UnitTypeUsage.NonRentable, false)]
    public void IsValidReservationUnitUsage_ShouldRequireReservable(UnitTypeUsage usage, bool expected)
        => Assert.Equal(expected, PropertyStructurePolicy.IsValidReservationUnitUsage(usage));

    [Theory]
    [InlineData(UnitTypeUsage.Reservable, true)]
    [InlineData(UnitTypeUsage.Rentable, false)]
    [InlineData(UnitTypeUsage.NonRentable, false)]
    public void RequiresChargeType_ShouldBeTrueOnlyForReservable(UnitTypeUsage usage, bool expected)
        => Assert.Equal(expected, PropertyStructurePolicy.RequiresChargeType(usage));

    [Theory]
    [InlineData(UnitTypeUsage.Reservable, 5, 5)]
    [InlineData(UnitTypeUsage.Rentable, 5, null)]
    [InlineData(UnitTypeUsage.NonRentable, 5, null)]
    [InlineData(UnitTypeUsage.Reservable, null, null)]
    public void ResolveChargeTypeId_ShouldAssignOnlyForReservable(
        UnitTypeUsage usage,
        int? chargeTypeId,
        int? expected)
        => Assert.Equal(expected, PropertyStructurePolicy.ResolveChargeTypeId(usage, chargeTypeId));
}

using KiraTakip.Domain.Leases;

namespace KiraTakip.Tests;

public class UnitCapacityPolicyTests
{
    private static readonly DateTime Start = new(2026, 1, 1);
    private static readonly DateTime End = new(2026, 12, 31);

    private static List<CapacityOccupant> Occupants(int count, decimal area, DateTime? start = null, DateTime? end = null)
        => Enumerable.Range(0, count)
            .Select(_ => new CapacityOccupant(start ?? Start, end ?? End, area))
            .ToList();

    [Fact]
    public void CalculateNormArea_ShouldDivideAreaByCapacity()
        => Assert.Equal(7m, UnitCapacityPolicy.CalculateNormArea(70m, 10));

    [Fact]
    public void CalculateNormArea_ShouldThrow_WhenCapacityNotPositive()
        => Assert.Throws<ArgumentOutOfRangeException>(() => UnitCapacityPolicy.CalculateNormArea(70m, 0));

    [Fact]
    public void SuggestLastSlotArea_ShouldReturnRemainder_WhenOneSlotLeft()
        => Assert.Equal(10m, UnitCapacityPolicy.SuggestLastSlotArea(10, 70m, 9, 60m));

    [Fact]
    public void SuggestLastSlotArea_ShouldReturnNull_WhenMoreSlotsLeft()
        => Assert.Null(UnitCapacityPolicy.SuggestLastSlotArea(10, 70m, 8, 50m));

    [Fact]
    public void GetPeakUsage_ShouldReturnHighestCountThenArea()
    {
        var mid = new DateTime(2026, 6, 15);
        var occupants = new List<CapacityOccupant>
        {
            new(Start, End, 10m),
            new(mid, End, 20m),
            new(mid, End, 5m)
        };

        var peak = UnitCapacityPolicy.GetPeakUsage(occupants, Start, End);

        Assert.Equal(new CapacityUsage(3, 35m), peak);
    }

    [Fact]
    public void GetPeakUsage_ShouldReturnZero_WhenNoOccupants()
        => Assert.Equal(new CapacityUsage(0, 0m), UnitCapacityPolicy.GetPeakUsage([], Start, End));

    [Theory]
    [InlineData(70, 3, 23.33)]
    [InlineData(100, 3, 33.33)]
    [InlineData(70, 14, 5.00)]
    [InlineData(70, 10, 7.00)]
    public void CalculateEffectiveNorm_ShouldFloorToTwoDecimals(double unitArea, int capacity, double expected)
        => Assert.Equal((decimal)expected, UnitCapacityPolicy.CalculateEffectiveNorm((decimal)unitArea, capacity));

    [Fact]
    public void CalculateEffectiveNorm_ShouldThrow_WhenCapacityNotPositive()
        => Assert.Throws<ArgumentOutOfRangeException>(() => UnitCapacityPolicy.CalculateEffectiveNorm(70m, 0));

    [Fact]
    public void T07_AutomaticAreas_ShouldSumBelowUnitArea_WhenNormFloored()
    {
        var norm = UnitCapacityPolicy.CalculateEffectiveNorm(100m, 3);
        var area = UnitCapacityPolicy.CalculateAutomaticArea(40m, norm);
        var occupants = Occupants(3, area);

        Assert.Equal(99.99m, occupants.Sum(o => o.AllocatedArea));
        Assert.Empty(UnitCapacityPolicy.GetHardViolations(3, 100m, occupants, Start, End));
    }

    [Theory]
    [InlineData(10, 7, 7)]
    [InlineData(5, 7, 5)]
    public void CalculateAutomaticArea_ShouldReturnMinOfRentedAndNorm(double rented, double norm, double expected)
        => Assert.Equal((decimal)expected, UnitCapacityPolicy.CalculateAutomaticArea((decimal)rented, (decimal)norm));

    [Fact]
    public void T04_GetCapacityLimit_ShouldFloorUnitAreaByMinimum()
    {
        Assert.Equal(14, UnitCapacityPolicy.GetCapacityLimit(70m, 5m));
        Assert.Equal(17, UnitCapacityPolicy.GetCapacityLimit(70m, 4m));
        Assert.Equal(int.MaxValue, UnitCapacityPolicy.GetCapacityLimit(70m, 0m));
    }

    [Fact]
    public void T05_ValidateArea_ShouldBeValid_WhenAllocatedBelowNormButAtMinimum()
        => Assert.Empty(UnitCapacityPolicy.ValidateArea(5m, 4m, 4m));

    [Fact]
    public void T06_ValidateArea_ShouldReturnRentedAndAllocatedBelowMinimum()
        => Assert.Equal(
            [LeaseAreaViolation.RentedBelowMinimum, LeaseAreaViolation.AllocatedBelowMinimum],
            UnitCapacityPolicy.ValidateArea(4m, 4m, 5m));

    [Fact]
    public void ValidateArea_ShouldReturnAllocatedAboveRented()
        => Assert.Equal(
            [LeaseAreaViolation.AllocatedAboveRented],
            UnitCapacityPolicy.ValidateArea(5m, 6m, 4m));

    [Fact]
    public void GetHardViolations_ShouldReturnCapacityExceeded_WhenCountAboveCapacity()
        => Assert.Equal(
            [CapacityHardViolation.CapacityExceeded],
            UnitCapacityPolicy.GetHardViolations(2, 70m, Occupants(3, 1m), Start, End));

    [Fact]
    public void GetHardViolations_ShouldReturnAreaExceeded_WhenAreaAboveUnitArea()
        => Assert.Equal(
            [CapacityHardViolation.AreaExceeded],
            UnitCapacityPolicy.GetHardViolations(10, 70m, Occupants(2, 40m), Start, End));

    [Fact]
    public void GetHardViolations_ShouldIgnoreOccupantsOutsideRange()
    {
        var occupants = Occupants(3, 40m, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        Assert.Empty(UnitCapacityPolicy.GetHardViolations(1, 70m, occupants, Start, End));
    }

    [Fact]
    public void GetHardViolations_ShouldNotReturnNormViolation_WhenFreeSlotsLackMinimumArea()
    {
        var occupants = Occupants(10, 6m);
        occupants.Add(new CapacityOccupant(Start, End, 5m));

        Assert.Empty(UnitCapacityPolicy.GetHardViolations(14, 70m, occupants, Start, End));
    }

    [Fact]
    public void T16_AutomaticAreas_ShouldFillUnitExactly_WhenRentedAboveNorm()
    {
        var norm = UnitCapacityPolicy.CalculateEffectiveNorm(70m, 10);
        var area = UnitCapacityPolicy.CalculateAutomaticArea(8m, norm);
        var occupants = Occupants(10, area);

        Assert.Equal(7m, area);
        Assert.Equal(70m, occupants.Sum(o => o.AllocatedArea));
        Assert.Empty(UnitCapacityPolicy.GetHardViolations(10, 70m, occupants, Start, End));
    }

    [Fact]
    public void T02_GetSnapshot_ShouldReportNormShortButMinimumMet()
    {
        var occupants = Occupants(9, 5m);
        occupants.Add(new CapacityOccupant(Start, End, 6m));

        var snapshot = UnitCapacityPolicy.GetSnapshot(14, 70m, 4m, occupants, Start);

        Assert.Equal(10, snapshot.OccupiedCount);
        Assert.Equal(51m, snapshot.AllocatedArea);
        Assert.Equal(4, snapshot.FreeSlots);
        Assert.Equal(19m, snapshot.RemainingArea);
        Assert.Equal(20m, snapshot.NormTarget);
        Assert.True(snapshot.NormTargetShort);
        Assert.Equal(16m, snapshot.MinimumTarget);
        Assert.False(snapshot.MinimumTargetShort);
    }

    [Fact]
    public void T14_GetSnapshot_ShouldReportBothShort_WhenRemainingBelowTargets()
    {
        var snapshot = UnitCapacityPolicy.GetSnapshot(14, 70m, 4m, Occupants(10, 6m), Start);

        Assert.Equal(10m, snapshot.RemainingArea);
        Assert.True(snapshot.NormTargetShort);
        Assert.True(snapshot.MinimumTargetShort);
    }

    [Fact]
    public void GetSnapshot_ShouldNotBeShort_WhenOfficeIsFull()
    {
        var snapshot = UnitCapacityPolicy.GetSnapshot(10, 70m, 4m, Occupants(10, 7m), Start);

        Assert.Equal(0, snapshot.FreeSlots);
        Assert.False(snapshot.NormTargetShort);
        Assert.False(snapshot.MinimumTargetShort);
    }
}

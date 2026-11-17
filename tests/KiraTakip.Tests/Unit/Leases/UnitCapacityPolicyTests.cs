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
    public void GetAllocatedAreaRange_ShouldReturnMinToRented_WhenRentedAboveNorm()
    {
        var range = UnitCapacityPolicy.GetAllocatedAreaRange(10m, 70m, 10, 5m);

        Assert.Equal((5m, 10m), range);
    }

    [Fact]
    public void GetAllocatedAreaRange_ShouldReturnRentedOnly_WhenRentedAtOrBelowNorm()
    {
        var range = UnitCapacityPolicy.GetAllocatedAreaRange(6m, 70m, 10, 5m);

        Assert.Equal((6m, 6m), range);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(8.5, true)]
    [InlineData(10, true)]
    [InlineData(4, false)]
    [InlineData(11, false)]
    public void IsAllocatedAreaAllowed_ShouldCheckRangeInclusive(double allocated, bool expected)
        => Assert.Equal(
            expected,
            UnitCapacityPolicy.IsAllocatedAreaAllowed((decimal)allocated, 10m, 70m, 10, 5m));

    [Fact]
    public void GetViolations_ShouldReturnCapacityExceeded_WhenActiveAndDraftOverlapWithCandidate()
    {
        var occupants = Occupants(8, 1m);
        occupants.AddRange(Occupants(2, 1m));
        occupants.Add(new CapacityOccupant(Start, End, 1m));

        var violations = UnitCapacityPolicy.GetViolations(10, 70m, 0m, occupants, Start, End);

        Assert.Equal([CapacityViolation.CapacityExceeded], violations);
    }

    [Fact]
    public void GetViolations_ShouldIgnoreOccupantsOutsideRange()
    {
        var occupants = Occupants(10, 1m, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));
        occupants.Add(new CapacityOccupant(Start, End, 1m));

        var violations = UnitCapacityPolicy.GetViolations(10, 70m, 0m, occupants, Start, End);

        Assert.Empty(violations);
    }

    [Fact]
    public void GetViolations_ShouldDetectPeakInMiddleOfRange()
    {
        var mid = new DateTime(2026, 6, 15);
        var occupants = new List<CapacityOccupant>
        {
            new(Start, End, 1m),
            new(mid, End, 1m),
            new(mid, End, 1m)
        };

        var violations = UnitCapacityPolicy.GetViolations(2, 70m, 0m, occupants, Start, End);

        Assert.Equal([CapacityViolation.CapacityExceeded], violations);
    }

    [Fact]
    public void GetViolations_ShouldReturnAreaExceeded_WhenSumAbovePhysicalArea()
    {
        var occupants = Occupants(2, 40m);

        var violations = UnitCapacityPolicy.GetViolations(2, 70m, 0m, occupants, Start, End);

        Assert.Equal([CapacityViolation.AreaExceeded], violations);
    }

    [Fact]
    public void GetViolations_ShouldReturnNormBelowMinimum_WhenFreeSlotsAverageBelowMinimum()
    {
        var occupants = Occupants(9, 66m / 9);

        var violations = UnitCapacityPolicy.GetViolations(10, 70m, 5m, occupants, Start, End);

        Assert.Equal([CapacityViolation.NormBelowMinimum], violations);
    }

    [Fact]
    public void GetViolations_ShouldNotApplyNormRule_WhenCapacityFull()
    {
        var occupants = Occupants(10, 7m);

        var violations = UnitCapacityPolicy.GetViolations(10, 70m, 5m, occupants, Start, End);

        Assert.Empty(violations);
    }

    [Fact]
    public void SuggestLastSlotArea_ShouldReturnRemainder_WhenOneSlotLeft()
        => Assert.Equal(10m, UnitCapacityPolicy.SuggestLastSlotArea(10, 70m, 9, 60m));

    [Fact]
    public void SuggestLastSlotArea_ShouldReturnNull_WhenMoreSlotsLeft()
        => Assert.Null(UnitCapacityPolicy.SuggestLastSlotArea(10, 70m, 8, 50m));

    [Fact]
    public void GetMaximumCapacity_ShouldAddFittingSlots()
        => Assert.Equal(11, UnitCapacityPolicy.GetMaximumCapacity(70m, 5m, 6, 42m));

    [Fact]
    public void GetMaximumCapacity_ShouldReturnMaxValue_WhenMinimumNotPositive()
        => Assert.Equal(int.MaxValue, UnitCapacityPolicy.GetMaximumCapacity(70m, 0m, 6, 42m));

    [Fact]
    public void GetMaximumCapacity_ShouldReturnOccupiedCount_WhenAreaOverused()
        => Assert.Equal(6, UnitCapacityPolicy.GetMaximumCapacity(70m, 5m, 6, 80m));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(6, 6)]
    public void GetMinimumCapacity_ShouldNotGoBelowOccupiedCountOrOne(int occupied, int expected)
        => Assert.Equal(expected, UnitCapacityPolicy.GetMinimumCapacity(occupied));

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
}

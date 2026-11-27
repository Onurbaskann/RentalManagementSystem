using KiraTakip.Models.Enums;

namespace KiraTakip.Domain.Leases;

public sealed record CapacityOccupant(
    DateTime StartDate,
    DateTime EndDate,
    decimal AllocatedArea,
    int? LeaseId,
    decimal? RentedArea)
{
    public CapacityOccupant(DateTime startDate, DateTime endDate, decimal allocatedArea)
        : this(startDate, endDate, allocatedArea, null, null)
    {
    }
}

public enum LeaseAreaViolation
{
    RentedBelowMinimum = 1,
    AllocatedBelowMinimum = 2,
    AllocatedAboveRented = 3
}

public enum CapacityHardViolation
{
    CapacityExceeded = 1,
    AreaExceeded = 2
}

public sealed record CapacitySnapshot(
    int OccupiedCount,
    decimal AllocatedArea,
    int FreeSlots,
    decimal RemainingArea,
    decimal NormTarget,
    decimal MinimumTarget)
{
    public bool NormTargetShort => FreeSlots > 0 && RemainingArea < NormTarget;

    public bool MinimumTargetShort => FreeSlots > 0 && RemainingArea < MinimumTarget;
}

public sealed record CapacityUsage(int OccupiedCount, decimal AllocatedArea);

public static class UnitCapacityPolicy
{
    public static decimal CalculateNormArea(decimal unitArea, int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        return unitArea / capacity;
    }

    public static decimal CalculateEffectiveNorm(decimal unitArea, int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        return Math.Floor(unitArea / capacity * 100m) / 100m;
    }

    public static decimal CalculateAutomaticArea(decimal rentedArea, decimal effectiveNorm)
        => Math.Min(rentedArea, effectiveNorm);

    public static int GetCapacityLimit(decimal unitArea, decimal minimumUsageArea)
        => minimumUsageArea <= 0 ? int.MaxValue : (int)Math.Floor(unitArea / minimumUsageArea);

    public static IReadOnlyList<LeaseAreaViolation> ValidateArea(
        decimal rentedArea,
        decimal allocatedArea,
        decimal minimumUsageArea)
    {
        var violations = new List<LeaseAreaViolation>();

        if (rentedArea < minimumUsageArea)
            violations.Add(LeaseAreaViolation.RentedBelowMinimum);

        if (allocatedArea < minimumUsageArea)
            violations.Add(LeaseAreaViolation.AllocatedBelowMinimum);

        if (allocatedArea > rentedArea)
            violations.Add(LeaseAreaViolation.AllocatedAboveRented);

        return violations;
    }

    public static IReadOnlyList<CapacityHardViolation> GetHardViolations(
        int capacity,
        decimal unitArea,
        IEnumerable<CapacityOccupant> occupants,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var list = occupants.ToList();
        var violations = new HashSet<CapacityHardViolation>();

        foreach (var day in GetEvaluationDays(list, rangeStart, rangeEnd))
        {
            var (count, area) = Measure(list, day);

            if (count > capacity)
                violations.Add(CapacityHardViolation.CapacityExceeded);

            if (area > unitArea)
                violations.Add(CapacityHardViolation.AreaExceeded);
        }

        return violations.OrderBy(v => (int)v).ToList();
    }

    public static CapacitySnapshot GetSnapshot(
        int capacity,
        decimal unitArea,
        decimal minimumUsageArea,
        IEnumerable<CapacityOccupant> occupants,
        DateTime day)
    {
        var (count, area) = Measure(occupants.ToList(), day.Date);
        var free = Math.Max(0, capacity - count);

        return new CapacitySnapshot(
            count,
            area,
            free,
            unitArea - area,
            free * CalculateEffectiveNorm(unitArea, capacity),
            free * minimumUsageArea);
    }

    public static CapacityUsage GetPeakUsage(
        IEnumerable<CapacityOccupant> occupants,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var list = occupants.ToList();
        var peak = new CapacityUsage(0, 0m);

        foreach (var day in GetEvaluationDays(list, rangeStart, rangeEnd))
        {
            var (count, area) = Measure(list, day);
            if (count > peak.OccupiedCount
                || (count == peak.OccupiedCount && area > peak.AllocatedArea))
                peak = new CapacityUsage(count, area);
        }

        return peak;
    }

    public static decimal? SuggestLastSlotArea(
        int capacity,
        decimal unitArea,
        int occupiedCount,
        decimal allocatedArea)
        => occupiedCount == capacity - 1 ? unitArea - allocatedArea : null;

    private static IEnumerable<DateTime> GetEvaluationDays(
        List<CapacityOccupant> occupants,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var start = rangeStart.Date;
        var end = rangeEnd.Date;
        var days = new HashSet<DateTime> { start };

        foreach (var occupant in occupants)
        {
            var occupantStart = occupant.StartDate.Date;
            if (occupantStart >= start && occupantStart <= end)
                days.Add(occupantStart);
        }

        return days.Where(d => d <= end).OrderBy(d => d);
    }

    private static (int Count, decimal Area) Measure(List<CapacityOccupant> occupants, DateTime day)
    {
        var count = 0;
        var area = 0m;
        foreach (var occupant in occupants)
        {
            if (occupant.StartDate.Date <= day && day <= occupant.EndDate.Date)
            {
                count++;
                area += occupant.AllocatedArea;
            }
        }

        return (count, area);
    }
}

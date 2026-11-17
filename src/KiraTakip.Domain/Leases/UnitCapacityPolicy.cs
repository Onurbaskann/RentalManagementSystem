namespace KiraTakip.Domain.Leases;

public sealed record CapacityOccupant(DateTime StartDate, DateTime EndDate, decimal AllocatedArea);

public enum CapacityViolation
{
    CapacityExceeded = 1,
    AreaExceeded = 2,
    NormBelowMinimum = 3
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

    public static (decimal Minimum, decimal Maximum) GetAllocatedAreaRange(
        decimal rentedArea,
        decimal unitArea,
        int capacity,
        decimal minimumUsageArea)
        => rentedArea > CalculateNormArea(unitArea, capacity)
            ? (minimumUsageArea, rentedArea)
            : (rentedArea, rentedArea);

    public static bool IsAllocatedAreaAllowed(
        decimal allocatedArea,
        decimal rentedArea,
        decimal unitArea,
        int capacity,
        decimal minimumUsageArea)
    {
        var (minimum, maximum) = GetAllocatedAreaRange(rentedArea, unitArea, capacity, minimumUsageArea);
        return allocatedArea >= minimum && allocatedArea <= maximum;
    }

    public static IReadOnlyList<CapacityViolation> GetViolations(
        int capacity,
        decimal unitArea,
        decimal minimumUsageArea,
        IEnumerable<CapacityOccupant> occupants,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var list = occupants.ToList();
        var violations = new HashSet<CapacityViolation>();

        foreach (var day in GetEvaluationDays(list, rangeStart, rangeEnd))
        {
            var (count, area) = Measure(list, day);

            if (count > capacity)
                violations.Add(CapacityViolation.CapacityExceeded);

            if (area > unitArea)
                violations.Add(CapacityViolation.AreaExceeded);

            var free = capacity - count;
            if (free > 0 && (unitArea - area) / free < minimumUsageArea)
                violations.Add(CapacityViolation.NormBelowMinimum);
        }

        return violations.OrderBy(v => (int)v).ToList();
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

    public static int GetMaximumCapacity(
        decimal unitArea,
        decimal minimumUsageArea,
        int occupiedCount,
        decimal allocatedArea)
    {
        if (minimumUsageArea <= 0)
            return int.MaxValue;

        var extra = (int)Math.Floor((unitArea - allocatedArea) / minimumUsageArea);
        return extra < 0 ? occupiedCount : occupiedCount + extra;
    }

    public static int GetMinimumCapacity(int occupiedCount)
        => Math.Max(1, occupiedCount);

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

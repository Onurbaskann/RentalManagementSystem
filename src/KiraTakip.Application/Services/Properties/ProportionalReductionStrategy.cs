using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Properties;

namespace KiraTakip.Services.Properties;

public sealed class ProportionalReductionStrategy : IAllocationSuggestionStrategy
{
    private const decimal Step = 0.01m;
    private const decimal StepScale = 100m;

    private readonly DefaultAllocationSuggestionStrategy baseStrategy = new();

    public AllocationSuggestionResult Suggest(AllocationSuggestionContext context)
    {
        var baseResult = baseStrategy.Suggest(context);
        var areas = baseResult.Suggestions.ToDictionary(item => item.LeaseId, item => item.AllocatedArea);
        var minimum = context.MinimumUsageArea;

        var candidates = context.Rows
            .Where(row => row.RentedArea.HasValue
                && row.CurrentMode == LeaseAreaMode.Manual
                && !context.Overrides.ContainsKey(row.LeaseId)
                && areas[row.LeaseId] > minimum)
            .ToList();

        var days = GetEvaluationDays(context);
        var alphas = new Dictionary<int, decimal>();

        foreach (var day in days)
        {
            var deficit = GetDeficit(context, areas, day);
            if (deficit <= 0) continue;

            var active = candidates.Where(row => IsActive(row, day)).ToList();
            var reducible = active.Sum(row => areas[row.LeaseId] - minimum);
            var alpha = reducible <= 0 ? 1m : Math.Min(1m, deficit / reducible);

            foreach (var row in active)
                alphas[row.LeaseId] = alphas.TryGetValue(row.LeaseId, out var existing)
                    ? Math.Max(existing, alpha)
                    : alpha;
        }

        var reduced = new Dictionary<int, decimal>(areas);
        foreach (var row in candidates)
        {
            if (!alphas.TryGetValue(row.LeaseId, out var alpha)) continue;
            var current = areas[row.LeaseId];
            var target = Math.Floor((current - alpha * (current - minimum)) * StepScale) / StepScale;
            reduced[row.LeaseId] = Math.Min(current, Math.Max(minimum, target));
        }

        var suggestions = baseResult.Suggestions
            .Select(item => item with { AllocatedArea = reduced[item.LeaseId] })
            .ToList();

        var shortfalls = days
            .Select(day => (Day: day, Deficit: GetDeficit(context, reduced, day)))
            .Where(item => item.Deficit > 0)
            .Select(item => new AllocationShortfall(item.Day, Math.Ceiling(item.Deficit * StepScale) * Step))
            .ToList();

        return new AllocationSuggestionResult(suggestions, shortfalls);
    }

    private static bool IsActive(AllocationSuggestionRow row, DateTime day)
        => row.StartDate.Date <= day && day <= row.EndDate.Date;

    private static List<DateTime> GetEvaluationDays(AllocationSuggestionContext context)
    {
        var today = context.Today.Date;
        var rangeEnd = context.Rows.Count == 0 ? today : context.Rows.Max(row => row.EndDate.Date);
        if (rangeEnd < today) rangeEnd = today;

        var days = new HashSet<DateTime> { today };
        foreach (var row in context.Rows)
        {
            var start = row.StartDate.Date;
            if (start >= today && start <= rangeEnd) days.Add(start);
        }

        return days.OrderBy(day => day).ToList();
    }

    private static decimal GetDeficit(
        AllocationSuggestionContext context,
        IReadOnlyDictionary<int, decimal> areas,
        DateTime day)
    {
        var active = context.Rows.Where(row => IsActive(row, day)).ToList();
        var free = Math.Max(0, context.Capacity - active.Count);
        var total = active.Sum(row => areas[row.LeaseId]) + free * context.EffectiveNorm;
        return Math.Max(0m, total - context.UnitArea);
    }
}

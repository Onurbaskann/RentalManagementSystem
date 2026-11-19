using KiraTakip.Models.Enums;

namespace KiraTakip.Services.Interfaces.Properties;

public record AllocationSuggestionRow(
    int LeaseId,
    decimal? RentedArea,
    decimal CurrentAllocatedArea,
    LeaseAreaMode? CurrentMode,
    DateTime StartDate,
    DateTime EndDate);

public record AllocationSuggestion(int LeaseId, decimal AllocatedArea, LeaseAreaMode? Mode);

public record AllocationShortfall(DateTime Day, decimal MissingArea);

public record AllocationSuggestionContext(
    decimal EffectiveNorm,
    decimal UnitArea,
    int Capacity,
    decimal MinimumUsageArea,
    DateTime Today,
    IReadOnlyList<AllocationSuggestionRow> Rows,
    IReadOnlyDictionary<int, decimal> Overrides);

public record AllocationSuggestionResult(
    IReadOnlyList<AllocationSuggestion> Suggestions,
    IReadOnlyList<AllocationShortfall> Shortfalls);

public static class AllocationStrategyKeys
{
    public const string ProportionalReduction = "AllocationStrategy.ProportionalReduction";
}

public interface IAllocationSuggestionStrategy
{
    AllocationSuggestionResult Suggest(AllocationSuggestionContext context);
}

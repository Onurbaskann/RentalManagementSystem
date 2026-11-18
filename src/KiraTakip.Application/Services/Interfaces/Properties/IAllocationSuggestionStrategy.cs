using KiraTakip.Models.Enums;

namespace KiraTakip.Services.Interfaces.Properties;

public record AllocationSuggestionRow(
    int LeaseId,
    decimal? RentedArea,
    decimal CurrentAllocatedArea,
    LeaseAreaMode? CurrentMode);

public record AllocationSuggestion(int LeaseId, decimal AllocatedArea, LeaseAreaMode? Mode);

public interface IAllocationSuggestionStrategy
{
    IReadOnlyList<AllocationSuggestion> Suggest(
        decimal effectiveNorm,
        IReadOnlyList<AllocationSuggestionRow> rows,
        IReadOnlyDictionary<int, decimal> overrides);
}

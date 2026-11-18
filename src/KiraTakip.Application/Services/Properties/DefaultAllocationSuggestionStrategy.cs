using KiraTakip.Domain.Leases;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Properties;

namespace KiraTakip.Services.Properties;

public sealed class DefaultAllocationSuggestionStrategy : IAllocationSuggestionStrategy
{
    public IReadOnlyList<AllocationSuggestion> Suggest(
        decimal effectiveNorm,
        IReadOnlyList<AllocationSuggestionRow> rows,
        IReadOnlyDictionary<int, decimal> overrides)
        => rows.Select(row =>
        {
            if (!row.RentedArea.HasValue)
                return new AllocationSuggestion(row.LeaseId, row.CurrentAllocatedArea, row.CurrentMode);

            if (overrides.TryGetValue(row.LeaseId, out var manual))
                return new AllocationSuggestion(row.LeaseId, manual, LeaseAreaMode.Manual);

            return row.CurrentMode == LeaseAreaMode.Automatic
                ? new AllocationSuggestion(
                    row.LeaseId,
                    UnitCapacityPolicy.CalculateAutomaticArea(row.RentedArea.Value, effectiveNorm),
                    LeaseAreaMode.Automatic)
                : new AllocationSuggestion(row.LeaseId, row.CurrentAllocatedArea, row.CurrentMode);
        }).ToList();
}

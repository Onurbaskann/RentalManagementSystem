using KiraTakip.Domain.Leases;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Properties;

namespace KiraTakip.Services.Properties;

public sealed class DefaultAllocationSuggestionStrategy : IAllocationSuggestionStrategy
{
    public AllocationSuggestionResult Suggest(AllocationSuggestionContext context)
        => new(
            context.Rows.Select(row =>
            {
                if (!row.RentedArea.HasValue)
                    return new AllocationSuggestion(row.LeaseId, row.CurrentAllocatedArea, row.CurrentMode);

                if (context.Overrides.TryGetValue(row.LeaseId, out var manual))
                    return new AllocationSuggestion(row.LeaseId, manual, LeaseAreaMode.Manual);

                return row.CurrentMode == LeaseAreaMode.Automatic
                    ? new AllocationSuggestion(
                        row.LeaseId,
                        UnitCapacityPolicy.CalculateAutomaticArea(row.RentedArea.Value, context.EffectiveNorm),
                        LeaseAreaMode.Automatic)
                    : new AllocationSuggestion(row.LeaseId, row.CurrentAllocatedArea, row.CurrentMode);
            }).ToList(),
            []);
}

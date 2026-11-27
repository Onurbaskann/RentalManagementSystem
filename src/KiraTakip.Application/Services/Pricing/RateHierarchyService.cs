using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Domain.Spaces;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Models.Dtos.RateHierarchy;

namespace KiraTakip.Services.Pricing;

public class RateHierarchyService(
    ISpaceRepository spaceRepository,
    ISpaceRateRepository spaceRateRepository,
    IRateScheduleRepository rateScheduleRepository,
    IReservationRateOverrideRepository reservationRateRepository) : IRateHierarchyService
{

    public async Task<ParentRateCardDto?> GetParentForAsync(GetParentRateInput input)
    {
        int targetYear = input.Year ?? DateTime.Now.Year;

        // Alan zinciri: Lease katmanında alanın kendisi + ataları, Space katmanında yalnız ataları (yakından uzağa).
        if (input.SpaceId.HasValue)
        {
            var paths = await spaceRepository.GetPathsAsync([input.SpaceId.Value]);
            if (paths.Count > 0)
            {
                var ancestors = SpaceTreePolicy.GetAncestorIds(paths[0]).Reverse().ToList();
                var chain = input.Layer == RateHierarchyLayer.Lease
                    ? ancestors.Prepend(input.SpaceId.Value).ToList()
                    : ancestors;

                if (chain.Count > 0)
                {
                    var rows = await spaceRateRepository.GetParentRowsAsync(chain, input.TenantCategoryId);
                    foreach (var spaceId in chain)
                    {
                        var hit = rows.Where(r => r.SpaceId == spaceId).Select(r => r.Row).ToList();
                        if (hit.Count == 0) continue;

                        var space = await spaceRepository.GetNoTrackingAsync(spaceId);
                        return new ParentRateCardDto(
                            space == null ? "Alan Tarifesi" : $"Alan Tarifesi - {space.Name}",
                            null,
                            hit);
                    }
                }
            }
        }

        // Her katman için sonuç: Genel Tarife
        var generalRows = await rateScheduleRepository.GetRowsByYearAndCategoryAsync(
            targetYear,
            input.TenantCategoryId);

        return new ParentRateCardDto(
            $"Genel Tarife - {targetYear}",
            null,
            generalRows);
    }

    public async Task<ParentReservationRateOverrideCardDto?> GetReservationParentAsync(int spaceId)
    {
        var rule = await reservationRateRepository.GetInheritedForSpaceAsync(spaceId);
        IReadOnlyList<ParentReservationRateOverrideRowDto> rows = rule == null
            ? []
            :
            [
                new ParentReservationRateOverrideRowDto(
                    rule.Space.Name,
                    rule.FreeDurationMinutes,
                    rule.BillingPeriodMinutes,
                    rule.PeriodRate,
                    rule.KdvRate)
            ];

        return new ParentReservationRateOverrideCardDto(
            "Rezervasyon Kuralı (en yakın üst alan)",
            null,
            rows);
    }
}

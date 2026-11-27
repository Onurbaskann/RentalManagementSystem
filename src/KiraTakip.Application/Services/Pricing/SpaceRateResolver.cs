using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Pricing;

namespace KiraTakip.Services.Pricing;

public class SpaceRateResolver(
    ISpaceRepository spaceRepository,
    ISpaceRateRepository spaceRateRepository,
    IRateScheduleRepository rateScheduleRepository) : ISpaceRateResolver
{
    public async Task<RateSnapshot?> ResolveAsync(int spaceId, int tenantCategoryId, int chargeTypeId, DateTime donem)
    {
        var paths = await spaceRepository.GetPathsAsync([spaceId]);
        if (paths.Count > 0)
        {
            // Yakından uzağa: alanın kendisi, sonra Path'in sondan başa doğru atları.
            var chain = SpaceTreePolicy.GetAncestorIds(paths[0]).Reverse().Prepend(spaceId).ToList();
            var rates = await spaceRateRepository.GetActiveRatesAsync(chain, tenantCategoryId, chargeTypeId);
            foreach (var id in chain)
            {
                var hit = rates.FirstOrDefault(r => r.SpaceId == id);
                if (hit.Rate != null)
                    return Wrap(hit.Rate, LineItemSourceType.SpaceRate);
            }
        }

        var kalem = await rateScheduleRepository.GetRateAsync(tenantCategoryId, chargeTypeId, donem.Year);
        return kalem == null ? null : Wrap(kalem, LineItemSourceType.RateSchedule);
    }

    private static RateSnapshot Wrap(RateValueDto v, LineItemSourceType kaynak) => new()
    {
        CalculationMethod = v.CalculationMethod,
        UnitValue = v.UnitValue,
        KdvRate = v.KdvRate,
        SourceType = kaynak
    };
}

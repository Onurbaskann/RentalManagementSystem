using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Pricing;

namespace KiraTakip.Services.Pricing;

public class BatchRateResolver(
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    ISpaceRepository spaceRepository,
    ISpaceRateRepository spaceRateRepository,
    IRateScheduleRepository rateScheduleRepository,
    ILeaseRepository leaseRepository) : IBatchRateResolver
{
    public async Task<Dictionary<(int LeaseId, int ChargeTypeId), RateSnapshot?>> ResolveManyAsync(
        IReadOnlyCollection<RateResolutionRequest> requests)
    {
        var result = new Dictionary<(int, int), RateSnapshot?>();
        if (requests.Count == 0) return result;

        var leaseIds = requests.Select(r => r.LeaseId).Distinct().ToList();
        var chargeTypeIds = requests.Select(r => r.ChargeTypeId).Distinct().ToList();

        // Kademe 0: sözleşme override — RateResolverService.ResolveAsync ile aynı.
        var leaseRateLookup = (await leaseRateOverrideRepository.GetRatesAsync(leaseIds, chargeTypeIds))
            .ToDictionary(r => (r.LeaseId, r.ChargeTypeId), r => r.Rate);

        var remaining = new List<RateResolutionRequest>();
        foreach (var request in requests)
        {
            var key = (request.LeaseId, request.ChargeTypeId);
            if (leaseRateLookup.TryGetValue(key, out var rate))
                result[key] = Wrap(rate, LineItemSourceType.LeaseRateOverride);
            else
                remaining.Add(request);
        }

        if (remaining.Count == 0) return result;

        // Sözleşmeden alan (yol dahil) + kiracı kategorisi: 1 sorgu. İstekteki SpaceId bilinçli yok sayılır;
        // alan her zaman sözleşmeden gelir (RateResolverService ile aynı).
        var spaceAndCategoryByLease = await leaseRepository.GetSpaceAndCategoriesAsync(
            remaining.Select(r => r.LeaseId).Distinct().ToList());

        var tenantCategoryIds = spaceAndCategoryByLease.Values
            .Select(v => v.TenantCategoryId)
            .Distinct()
            .ToList();

        // Alan zinciri (alan + atalar, SpaceRateResolver ile aynı sıra) için tek toplu sorgu.
        // Silinmiş alan için zincir kurulmaz (tekil çözücüyle aynı: doğrudan genel tarife). Tek sorgu.
        var livePaths = (await spaceRepository.GetPathsAsync(
                spaceAndCategoryByLease.Values.Select(v => v.SpaceId).Distinct().ToList()))
            .ToHashSet(StringComparer.Ordinal);
        var chainByLease = spaceAndCategoryByLease.ToDictionary(
            kv => kv.Key,
            kv => livePaths.Contains(kv.Value.SpacePath)
                ? SpaceTreePolicy.GetAncestorIds(kv.Value.SpacePath).Reverse().Prepend(kv.Value.SpaceId).ToList()
                : new List<int>());

        var chainSpaceIds = chainByLease.Values.SelectMany(c => c).Distinct().ToList();
        var spaceRateLookup = tenantCategoryIds.Count == 0 || chainSpaceIds.Count == 0
            ? new Dictionary<(int SpaceId, int TenantCategoryId, int ChargeTypeId), RateValueDto>()
            : (await spaceRateRepository.GetActiveRatesAsync(chainSpaceIds, tenantCategoryIds, chargeTypeIds))
                .ToDictionary(r => (r.SpaceId, r.TenantCategoryId, r.ChargeTypeId), r => r.Rate);

        // Genel tarife (yıl düşmesi bellekte, D6): 1 sorgu.
        var scheduleByKey = tenantCategoryIds.Count == 0
            ? new Dictionary<(int, int), List<(int TenantCategoryId, int ChargeTypeId, int Year, RateValueDto Rate)>>()
            : (await rateScheduleRepository.GetRatesAsync(tenantCategoryIds, chargeTypeIds))
                .GroupBy(r => (r.TenantCategoryId, r.ChargeTypeId))
                .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var request in remaining)
        {
            var key = (request.LeaseId, request.ChargeTypeId);

            if (!spaceAndCategoryByLease.TryGetValue(request.LeaseId, out var info))
            {
                result[key] = null;
                continue;
            }

            var tenantCategoryId = info.TenantCategoryId;

            RateSnapshot? snapshot = null;
            foreach (var spaceId in chainByLease[request.LeaseId])
            {
                if (spaceRateLookup.TryGetValue((spaceId, tenantCategoryId, request.ChargeTypeId), out var spaceRate))
                {
                    snapshot = Wrap(spaceRate, LineItemSourceType.SpaceRate);
                    break;
                }
            }

            if (snapshot == null && scheduleByKey.TryGetValue((tenantCategoryId, request.ChargeTypeId), out var scheduleRows))
            {
                var best = scheduleRows
                    .OrderByDescending(r => r.Year == request.Period.Year ? 1 : 0)
                    .ThenByDescending(r => r.Year)
                    .First();
                snapshot = Wrap(best.Rate, LineItemSourceType.RateSchedule);
            }

            result[key] = snapshot;
        }

        return result;
    }

    private static RateSnapshot Wrap(RateValueDto v, LineItemSourceType source)
        => new()
        {
            CalculationMethod = v.CalculationMethod,
            UnitValue = v.UnitValue,
            KdvRate = v.KdvRate,
            SourceType = source
        };
}

using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Services.Interfaces.Pricing;

namespace KiraTakip.Services.Pricing;

public class BatchRateResolver(
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IUnitRateRepository unitRateRepository,
    IPropertyRateOverrideRepository propertyRateOverrideRepository,
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

        // Kademe 0: sözleşme override — RateResolverService.ResolveAsync satır 21-26 ile aynı.
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

        // ResolveAsync satır 31-39: sözleşmeden taşınmaz + kiracı kategorisi.
        var propertyAndCategoryByLease = await leaseRepository.GetPropertyAndCategoriesAsync(
            remaining.Select(r => r.LeaseId).Distinct().ToList());

        var tenantCategoryIds = propertyAndCategoryByLease.Values
            .Where(v => v.TenantCategoryId.HasValue)
            .Select(v => v.TenantCategoryId!.Value)
            .Distinct()
            .ToList();

        // Kademe 1: birim rate — ResolveAsync satır 46-50.
        var unitRateLookup = tenantCategoryIds.Count == 0
            ? new Dictionary<(int, int, int), RateValueDto>()
            : (await unitRateRepository.GetRatesAsync(
                    remaining.Select(r => r.UnitId).Distinct().ToList(),
                    tenantCategoryIds,
                    chargeTypeIds))
                .ToDictionary(r => (r.UnitId, r.TenantCategoryId, r.ChargeTypeId), r => r.Rate);

        // Kademe 2: taşınmaz rate — ResolveAsync satır 53-58 (tek taşınmaz varsayımı; Property/Details
        // tek taşınmaza ait olduğu için pratikte 1 sorgu; birden çok taşınmaz olursa taşınmaz başına 1 sorgu).
        var propertyRateLookup = new Dictionary<(int PropertyId, int TenantCategoryId, int ChargeTypeId), RateValueDto>();
        foreach (var propertyId in propertyAndCategoryByLease.Values.Select(v => v.PropertyId).Distinct())
        {
            foreach (var rate in await propertyRateOverrideRepository.GetActiveRatesAsync(propertyId))
                propertyRateLookup[(propertyId, rate.TenantCategoryId, rate.ChargeTypeId)] = rate.Rate;
        }

        // Kademe 3: genel tarife — ResolveAsync satır 60-65 (yıl-fallback sıralaması bellekte).
        var scheduleByKey = tenantCategoryIds.Count == 0
            ? new Dictionary<(int, int), List<(int TenantCategoryId, int ChargeTypeId, int Year, RateValueDto Rate)>>()
            : (await rateScheduleRepository.GetRatesAsync(tenantCategoryIds, chargeTypeIds))
                .GroupBy(r => (r.TenantCategoryId, r.ChargeTypeId))
                .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var request in remaining)
        {
            var key = (request.LeaseId, request.ChargeTypeId);

            if (!propertyAndCategoryByLease.TryGetValue(request.LeaseId, out var info))
            {
                result[key] = null;
                continue;
            }

            var (propertyId, tenantCategoryId) = info;

            if (tenantCategoryId.HasValue
                && unitRateLookup.TryGetValue((request.UnitId, tenantCategoryId.Value, request.ChargeTypeId), out var unitRate))
            {
                result[key] = Wrap(unitRate, LineItemSourceType.UnitRateOverride);
                continue;
            }

            if (tenantCategoryId.HasValue
                && propertyRateLookup.TryGetValue((propertyId, tenantCategoryId.Value, request.ChargeTypeId), out var propertyRate))
            {
                result[key] = Wrap(propertyRate, LineItemSourceType.PropertyRateOverride);
                continue;
            }

            if (!tenantCategoryId.HasValue)
            {
                result[key] = null;
                continue;
            }

            if (scheduleByKey.TryGetValue((tenantCategoryId.Value, request.ChargeTypeId), out var scheduleRows))
            {
                var best = scheduleRows
                    .OrderByDescending(r => r.Year == request.Period.Year ? 1 : 0)
                    .ThenByDescending(r => r.Year)
                    .First();
                result[key] = Wrap(best.Rate, LineItemSourceType.RateSchedule);
            }
            else
            {
                result[key] = null;
            }
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

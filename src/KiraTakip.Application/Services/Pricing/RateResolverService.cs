using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces.Pricing;

namespace KiraTakip.Services.Pricing;

public class RateResolverService(
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    ISpaceRateResolver spaceRateResolver,
    ILeaseRepository sozlesmeRepository,
    ITenantRepository tenantRepository) : IRateResolverService
{
    // Sıra: sözleşme override > alan zinciri (alan, sonra atalar) > genel tarife (ISpaceRateResolver).
    public async Task<RateSnapshot?> ResolveAsync(int? leaseId, int? tenantId, int spaceId, int chargeTypeId, DateTime donem)
    {
        if (leaseId.HasValue)
        {
            var sozRate = await leaseRateOverrideRepository.GetRateAsync(leaseId.Value, chargeTypeId);
            if (sozRate != null)
                return new RateSnapshot
                {
                    CalculationMethod = sozRate.CalculationMethod,
                    UnitValue = sozRate.UnitValue,
                    KdvRate = sozRate.KdvRate,
                    SourceType = LineItemSourceType.LeaseRateOverride
                };
        }

        int? tenantCategoryId = null;

        if (leaseId.HasValue)
        {
            var info = await sozlesmeRepository.GetSpaceAndCategoryAsync(leaseId.Value);
            if (info != null)
            {
                tenantCategoryId = info.Value.TenantCategoryId;
                // Sözleşmeli çağrıda alan sözleşmeden gelir (BatchRateResolver ile aynı); girdi spaceId yok sayılır.
                spaceId = info.Value.SpaceId;
            }
        }
        else if (tenantId.HasValue)
        {
            tenantCategoryId = await tenantRepository.GetCategoryIdAsync(tenantId.Value);
        }

        if (!tenantCategoryId.HasValue) return null;

        return await spaceRateResolver.ResolveAsync(spaceId, tenantCategoryId.Value, chargeTypeId, donem);
    }
}

using KiraTakip.Models;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RateResolverService(
    ILeaseRateOverrideRepository leaseRateOverrideRepository,
    IUnitRateRepository unitRateRepository,
    IPropertyRateOverrideRepository propertyRateOverrideRepository,
    IRateScheduleRepository rateScheduleRepository,
    ILeaseRepository sozlesmeRepository,
    IUnitRepository unitRepository,
    ITenantRepository tenantRepository) : IRateResolverService
{
    public async Task<RateSnapshot?> ResolveAsync(int? leaseId, int? tenantId, int unitId, int chargeTypeId, DateTime donem)
    {
        if (leaseId.HasValue)
        {
            var sozRate = await leaseRateOverrideRepository.GetRateAsync(leaseId.Value, chargeTypeId);
            if (sozRate != null)
                return Wrap(sozRate, LineItemSourceType.LeaseRateOverride);
        }

        int? propertyId = null;
        int? tenantCategoryId = null;

        if (leaseId.HasValue)
        {
            var info = await sozlesmeRepository.GetPropertyAndCategoryAsync(leaseId.Value);
            if (info != null)
            {
                propertyId = info.Value.TasinmazId;
                tenantCategoryId = info.Value.KategoriId;
            }
        }
        else if (tenantId.HasValue)
        {
            propertyId = await unitRepository.GetPropertyIdAsync(unitId);
            tenantCategoryId = await tenantRepository.GetCategoryIdAsync(tenantId.Value);
        }

        if (tenantCategoryId.HasValue)
        {
            var birimRate = await unitRateRepository.GetRateAsync(unitId, tenantCategoryId.Value, chargeTypeId);
            if (birimRate != null)
                return Wrap(birimRate, LineItemSourceType.UnitRateOverride);
        }

        if (propertyId.HasValue && tenantCategoryId.HasValue)
        {
            var fiyatMatrisi = await propertyRateOverrideRepository.GetRateAsync(propertyId.Value, tenantCategoryId.Value, chargeTypeId);
            if (fiyatMatrisi != null)
                return Wrap(fiyatMatrisi, LineItemSourceType.PropertyRateOverride);
        }

        if (!tenantCategoryId.HasValue) return null;

        var kalem = await rateScheduleRepository.GetRateAsync(tenantCategoryId.Value, chargeTypeId, donem.Year);
        if (kalem == null) return null;

        return Wrap(kalem, LineItemSourceType.RateSchedule);
    }

    private static RateSnapshot Wrap(Models.Dtos.RateValueDto v, LineItemSourceType kaynak)
         => new RateSnapshot
         {
             CalculationMethod = v.CalculationMethod,
             UnitValue = v.UnitValue,
             KdvRate = v.KdvRate,
             SourceType = kaynak
         };
}

using KiraTakip.Models;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RateResolverService : IRateResolverService
{
    private readonly ISozlesmeTarifeRepository _sozlesmeTarifeRepo;
    private readonly IUnitRateRepository _unitRateRepo;
    private readonly ITasinmazTarifeRepository _tasinmazTarifeRepo;
    private readonly IGenelTarifeRepository _genelTarifeRepo;
    private readonly ILeaseRepository _sozlesmeRepo;
    private readonly IUnitRepository _birimRepo;
    private readonly ITenantRepository _kiraciRepo;

    public RateResolverService(
        ISozlesmeTarifeRepository sozlesmeTarifeRepo,
        IUnitRateRepository unitRateRepo,
        ITasinmazTarifeRepository tasinmazTarifeRepo,
        IGenelTarifeRepository genelTarifeRepo,
        ILeaseRepository sozlesmeRepo,
        IUnitRepository birimRepo,
        ITenantRepository kiraciRepo)
    {
        _sozlesmeTarifeRepo = sozlesmeTarifeRepo;
        _unitRateRepo = unitRateRepo;
        _tasinmazTarifeRepo = tasinmazTarifeRepo;
        _genelTarifeRepo = genelTarifeRepo;
        _sozlesmeRepo = sozlesmeRepo;
        _birimRepo = birimRepo;
        _kiraciRepo = kiraciRepo;
    }

    public async Task<RateSnapshot?> ResolveAsync(int? leaseId, int? tenantId, int unitId, int chargeTypeId, DateTime donem)
    {
        if (leaseId.HasValue)
        {
            var sozRate = await _sozlesmeTarifeRepo.GetRateAsync(leaseId.Value, chargeTypeId);
            if (sozRate != null)
                return Wrap(sozRate, LineItemSourceType.LeaseRateOverride);
        }

        int? propertyId = null;
        int? tenantCategoryId = null;

        if (leaseId.HasValue)
        {
            var info = await _sozlesmeRepo.GetPropertyAndCategoryAsync(leaseId.Value);
            if (info != null)
            {
                propertyId = info.Value.TasinmazId;
                tenantCategoryId = info.Value.KategoriId;
            }
        }
        else if (tenantId.HasValue)
        {
            propertyId = await _birimRepo.GetPropertyIdAsync(unitId);
            tenantCategoryId = await _kiraciRepo.GetKategoriIdAsync(tenantId.Value);
        }

        if (tenantCategoryId.HasValue)
        {
            var birimRate = await _unitRateRepo.GetRateAsync(unitId, tenantCategoryId.Value, chargeTypeId);
            if (birimRate != null)
                return Wrap(birimRate, LineItemSourceType.UnitRateOverride);
        }

        if (propertyId.HasValue && tenantCategoryId.HasValue)
        {
            var fiyatMatrisi = await _tasinmazTarifeRepo.GetRateAsync(propertyId.Value, tenantCategoryId.Value, chargeTypeId);
            if (fiyatMatrisi != null)
                return Wrap(fiyatMatrisi, LineItemSourceType.PropertyRateOverride);
        }

        if (!tenantCategoryId.HasValue) return null;

        var kalem = await _genelTarifeRepo.GetRateAsync(tenantCategoryId.Value, chargeTypeId, donem.Year);
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

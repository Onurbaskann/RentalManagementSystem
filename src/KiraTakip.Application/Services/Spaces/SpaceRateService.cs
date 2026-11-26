using KiraTakip.Data;
using KiraTakip.Domain.Pricing;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Catalog;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Spaces;

namespace KiraTakip.Services.Spaces;

public class SpaceRateService(
    ISpaceRateRepository rateRepository,
    ISpaceRepository spaceRepository,
    ICategoryRepository categoryRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitOfWork uow) : ISpaceRateService
{
    private const string ValueField = "UnitValue";

    public async Task<List<SpaceRateDto>> GetListAsync(int spaceId)
        => (await rateRepository.GetBySpaceAsync(spaceId))
            .Select(r => new SpaceRateDto(
                r.Id, r.SpaceId, r.TenantCategoryId, r.ChargeTypeId, r.CalculationMethod, r.UnitValue, r.KdvRate))
            .ToList();

    public async Task<int> SaveAsync(SpaceRateInput input)
    {
        Guard.NotFound(await spaceRepository.GetNoTrackingAsync(input.SpaceId), "Alan bulunamadı.");
        Guard.NotFound(await categoryRepository.GetByIdAsync(input.TenantCategoryId), "Kiracı kategorisi bulunamadı.");
        var chargeType = Guard.NotFound(await chargeTypeRepository.GetByIdAsync(input.ChargeTypeId), "Borç tipi bulunamadı.");

        Guard.InvalidField(
            !RateValuePolicy.IsValid(input.UnitValue, input.KdvRate, input.CalculationMethod, chargeType.Behavior),
            ValueField,
            "Tarife değerleri geçersiz.",
            "SPACE_RATE_INVALID_VALUE");

        var rate = await rateRepository.GetByKeyAsync(input.SpaceId, input.TenantCategoryId, input.ChargeTypeId);
        if (rate == null)
        {
            rate = new SpaceRate
            {
                SpaceId = input.SpaceId,
                TenantCategoryId = input.TenantCategoryId,
                ChargeTypeId = input.ChargeTypeId
            };
            await rateRepository.AddAsync(rate);
        }
        rate.CalculationMethod = input.CalculationMethod;
        rate.UnitValue = input.UnitValue;
        rate.KdvRate = input.KdvRate;
        await uow.SaveChangesAsync();
        return rate.Id;
    }

    public async Task DeleteAsync(int rateId)
    {
        Guard.NotFound(await rateRepository.GetByIdAsync(rateId), "Tarife bulunamadı.");
        await rateRepository.DeleteAsync(rateId);
        await uow.SaveChangesAsync();
    }
}

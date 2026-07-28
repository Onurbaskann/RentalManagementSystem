using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PropertyPricingService(
    IPropertyRateOverrideRepository propertyRateRepository,
    IUnitOfWork unitOfWork) : IPropertyPricingService
{
    public async Task<PropertyPricingMatrixDto> GetMatrixAsync(GetPropertyPricingMatrixInput input)
    {
        Guard.Forbidden(
            input.PropertyId > 0
                && input.AccessiblePropertyIds != null
                && !input.AccessiblePropertyIds.Contains(input.PropertyId),
            "Bu taşınmazın fiyat parametrelerini görüntüleme yetkiniz bulunmuyor.",
            "PropertyPricing.OutOfScope");

        var context = await propertyRateRepository.GetPricingContextAsync(input.PropertyId);
        Guard.NotFound(
            context.PropertyExists ? context : null,
            "Taşınmaz bulunamadı.",
            "Property.NotFound");

        var matrix = new PropertyPricingMatrixDto
        {
            PropertyId = input.PropertyId,
            PropertyName = context.PropertyName,
            CurrentPage = input.Page,
            PageSize = input.PageSize,
            Columns = context.ChargeTypes.Select(chargeType => new PropertyPricingColumnDto(
                chargeType.Id,
                chargeType.Name,
                chargeType.Code,
                chargeType.Behavior)).ToList()
        };

        var rows = new List<PropertyPricingRowDto>();
        foreach (var category in context.Categories)
        {
            var row = new PropertyPricingRowDto
            {
                TenantCategoryId = category.Id,
                TenantCategoryName = category.Name
            };

            foreach (var chargeType in context.ChargeTypes)
            {
                var rate = context.Rates.FirstOrDefault(item =>
                    item.TenantCategoryId == category.Id
                    && item.ChargeTypeId == chargeType.Id);
                row.Cells.Add(new PropertyPricingCellDto
                {
                    PropertyRateOverrideId = rate?.Id,
                    PropertyId = input.PropertyId,
                    TenantCategoryId = category.Id,
                    ChargeTypeId = chargeType.Id,
                    UnitValue = rate?.UnitValue,
                    CalculationMethod = rate?.CalculationMethod
                        ?? (chargeType.Code == BorcTipiConsts.Kira
                            ? CalculationMethod.M2
                            : CalculationMethod.Fixed),
                    VatRate = rate?.VatRate,
                    HasRate = rate != null
                });
            }
            rows.Add(row);
        }

        matrix.TotalRows = rows.Count;
        matrix.Rows = rows
            .Skip((input.Page - 1) * input.PageSize)
            .Take(input.PageSize)
            .ToList();

        return matrix;
    }

    public async Task SaveMatrixAsync(SavePropertyPricingMatrixInput input)
    {
        var context = await propertyRateRepository.GetPricingContextAsync(input.PropertyId);
        Guard.NotFound(
            context.PropertyExists ? context : null,
            "Taşınmaz bulunamadı.",
            "Property.NotFound");

        var validCategoryIds = context.Categories.Select(category => category.Id).ToHashSet();
        var validChargeTypeIds = context.ChargeTypes.Select(chargeType => chargeType.Id).ToHashSet();
        var submittedPairs = new HashSet<(int CategoryId, int ChargeTypeId)>();
        var existingRates = new Dictionary<int, PropertyRateOverride>();

        foreach (var row in input.Rows)
        {
            foreach (var cell in row.Cells)
            {
                Guard.InvalidField(
                    row.TenantCategoryId != cell.TenantCategoryId
                        || !validCategoryIds.Contains(cell.TenantCategoryId)
                        || !validChargeTypeIds.Contains(cell.ChargeTypeId)
                        || !submittedPairs.Add((cell.TenantCategoryId, cell.ChargeTypeId)),
                    "PricingMatrix",
                    "Fiyat matrisinde geçersiz veya yinelenen bir hücre bulunuyor.",
                    "Property.InvalidPricingCell");
                Guard.InvalidField(
                    cell.UnitValue < 0 || cell.VatRate is < 0 or > 100
                        || !Enum.IsDefined(cell.CalculationMethod),
                    "PricingMatrix",
                    "Fiyat matrisi değerlerinden biri geçersiz.",
                    "Property.InvalidPricingValue");

                if (!cell.PropertyRateOverrideId.HasValue) continue;
                var rate = Guard.NotFound(
                    await propertyRateRepository.GetByIdAsync(cell.PropertyRateOverrideId.Value),
                    "Fiyat kaydı bulunamadı.",
                    "Property.PricingRateNotFound");
                Guard.Forbidden(
                    rate.PropertyId != input.PropertyId
                        || rate.TenantCategoryId != cell.TenantCategoryId
                        || rate.ChargeTypeId != cell.ChargeTypeId,
                    "Fiyat kaydı bu taşınmaza veya hücreye ait değil.",
                    "Property.ForeignPricingRate");
                existingRates[rate.Id] = rate;
            }
        }

        foreach (var row in input.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.PropertyRateOverrideId.HasValue)
                {
                    var entity = existingRates[cell.PropertyRateOverrideId.Value];
                    if (cell.UnitValue.HasValue)
                    {
                        entity.UnitValue = cell.UnitValue.Value;
                        entity.CalculationMethod = cell.CalculationMethod;
                        entity.KdvRate = cell.VatRate ?? 0m;
                    }
                    else
                    {
                        await propertyRateRepository.DeleteAsync(entity.Id);
                    }
                }
                else if (cell.UnitValue.HasValue)
                {
                    await propertyRateRepository.AddAsync(new PropertyRateOverride
                    {
                        PropertyId = input.PropertyId,
                        TenantCategoryId = cell.TenantCategoryId,
                        ChargeTypeId = cell.ChargeTypeId,
                        UnitValue = cell.UnitValue.Value,
                        CalculationMethod = cell.CalculationMethod,
                        KdvRate = cell.VatRate ?? 0m
                    });
                }
            }
        }

        await unitOfWork.SaveChangesAsync();
    }
}

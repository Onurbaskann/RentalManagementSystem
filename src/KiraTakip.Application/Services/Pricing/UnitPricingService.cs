using KiraTakip.Data;
using KiraTakip.Domain.Pricing;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Pricing;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Pricing;
using KiraTakip.Models.Dtos.RateHierarchy;

namespace KiraTakip.Services.Pricing;

public class UnitPricingService(
    IUnitRateRepository unitRateRepository,
    IReservationRateOverrideRepository reservationRateOverrideRepository,
    IRateHierarchyService rateHierarchyService,
    IUnitOfWork unitOfWork) : IUnitPricingService
{
    public async Task<UnitPricingDataDto> GetPricingMatrixAsync(GetUnitPricingInput input)
    {
        var context = await unitRateRepository.GetPricingContextAsync(input.UnitId, input.Year);
        Guard.NotFound(
            context.UnitExists ? context : null,
            "Birim bulunamadı.",
            "UNIT_PRICING_UNIT_NOT_FOUND");
        EnsureScope(context.PropertyId, context.UnitId, input.AccessScope);

        var columns = new List<UnitPricingColumnDto>();
        var rows = new List<UnitPricingCategoryRowDto>();
        ParentRateCardDto? parentRate = null;
        ReservationRateOverride? customReservationRule = null;
        ParentReservationRateOverrideCardDto? parentReservationRateOverride = null;

        var isLeasable = context.UnitTypeUsage == UnitTypeUsage.Rentable;
        var isReservable = context.UnitTypeUsage == UnitTypeUsage.Reservable;

        if (isLeasable)
        {
            var parentRateRows = new List<ParentRateRowDto>();
            foreach (var category in context.Categories)
            {
                foreach (var chargeType in context.ChargeTypes)
                {
                    var propertyRate = context.PropertyRates.FirstOrDefault(rate =>
                        rate.TenantCategoryId == category.Id
                        && rate.ChargeTypeId == chargeType.Id);
                    var generalRate = context.GeneralRates.FirstOrDefault(rate =>
                        rate.TenantCategoryId == category.Id
                        && rate.ChargeTypeId == chargeType.Id);
                    var effectiveRate = propertyRate ?? generalRate;
                    if (effectiveRate == null) continue;

                    parentRateRows.Add(new ParentRateRowDto(
                        category.Name,
                        chargeType.Name,
                        effectiveRate.CalculationMethod,
                        effectiveRate.UnitValue,
                        effectiveRate.VatRate,
                        propertyRate != null ? "Taşınmaz Tarifesi" : "Genel Tarife"));
                }
            }

            parentRate = new ParentRateCardDto(
                "Yürürlükteki Üst Tarifeler (Varsayılanlar)",
                "Taşınmaz ve Genel Tarifelerin birleşimi",
                parentRateRows);

            columns = context.ChargeTypes.Select(chargeType => new UnitPricingColumnDto
            {
                ChargeTypeId = chargeType.Id,
                ChargeTypeName = chargeType.Name,
                ChargeTypeCode = chargeType.Code,
                ChargeTypeBehavior = chargeType.Behavior
            }).ToList();

            rows = context.Categories.Select(category => new UnitPricingCategoryRowDto
            {
                TenantCategoryId = category.Id,
                TenantCategoryName = category.Name,
                Cells = context.ChargeTypes.Select(chargeType =>
                {
                    var rate = context.Rates.FirstOrDefault(item =>
                        item.TenantCategoryId == category.Id
                        && item.ChargeTypeId == chargeType.Id);
                    var propertyRate = context.PropertyRates.FirstOrDefault(item =>
                        item.TenantCategoryId == category.Id
                        && item.ChargeTypeId == chargeType.Id);
                    var generalRate = context.GeneralRates.FirstOrDefault(item =>
                        item.TenantCategoryId == category.Id
                        && item.ChargeTypeId == chargeType.Id);
                    var effectiveRate = propertyRate ?? generalRate;
                    var defaultCalculationMethod = chargeType.Code == BorcTipiConsts.Kira
                        ? CalculationMethod.M2
                        : CalculationMethod.Fixed;

                    return new UnitPricingCellDto
                    {
                        TenantCategoryId = category.Id,
                        ChargeTypeId = chargeType.Id,
                        IsCustomRateActive = rate != null,
                        CalculationMethod = rate?.CalculationMethod ?? defaultCalculationMethod,
                        UnitValue = rate?.UnitValue ?? 0,
                        VatRate = rate?.VatRate ?? 0,
                        DefaultUnitValue = effectiveRate?.UnitValue ?? 0,
                        DefaultVatRate = effectiveRate?.VatRate ?? 0,
                        DefaultCalculationMethod = effectiveRate?.CalculationMethod
                            ?? defaultCalculationMethod,
                        DefaultSource = propertyRate != null
                            ? "Taşınmaz Tarifesi"
                            : generalRate != null
                                ? "Genel Tarife"
                                : "Tanımsız"
                    };
                }).ToList()
            }).ToList();
        }
        else if (isReservable)
        {
            customReservationRule = await reservationRateOverrideRepository.GetForUnitAsync(input.UnitId);
            parentReservationRateOverride = await rateHierarchyService.GetReservationParentAsync(
                new GetParentReservationRateInput(input.Year));
        }

        return new UnitPricingDataDto(
            context.UnitId,
            context.UnitName,
            context.PropertyId,
            context.PropertyName,
            isLeasable,
            isReservable,
            context.UnitTypeName,
            columns,
            rows,
            parentRate,
            customReservationRule,
            parentReservationRateOverride);
    }

    public async Task SavePricingMatrixAsync(SaveUnitPricingInput input)
    {
        var context = await unitRateRepository.GetPricingContextAsync(
            input.UnitId,
            DateTime.Now.Year);
        Guard.NotFound(
            context.UnitExists ? context : null,
            "Birim bulunamadı.",
            "UNIT_PRICING_UNIT_NOT_FOUND");
        EnsureScope(context.PropertyId, context.UnitId, input.AccessScope);
        Guard.InvalidField(
            context.UnitTypeUsage != UnitTypeUsage.Rentable,
            nameof(input.Rows),
            "Yalnızca kiralanabilir birimler için özel fiyat matrisi kaydedilebilir.",
            "UNIT_PRICING_NOT_RENTABLE");

        var validCategoryIds = context.Categories.Select(category => category.Id).ToHashSet();
        var chargeTypes = context.ChargeTypes.ToDictionary(chargeType => chargeType.Id);
        var expectedPairs = validCategoryIds
            .SelectMany(categoryId => chargeTypes.Keys.Select(chargeTypeId =>
                (TenantCategoryId: categoryId, ChargeTypeId: chargeTypeId)))
            .ToHashSet();
        var submittedPairs = new HashSet<(int TenantCategoryId, int ChargeTypeId)>();

        foreach (var row in input.Rows)
        {
            foreach (var cell in row.Cells)
            {
                var isValidCell = row.TenantCategoryId == cell.TenantCategoryId
                    && validCategoryIds.Contains(cell.TenantCategoryId)
                    && chargeTypes.ContainsKey(cell.ChargeTypeId)
                    && submittedPairs.Add((cell.TenantCategoryId, cell.ChargeTypeId));
                Guard.InvalidField(
                    !isValidCell,
                    nameof(input.Rows),
                    "Fiyat matrisinde geçersiz veya yinelenen bir hücre bulunuyor.",
                    "UNIT_PRICING_INVALID_CELL");

                if (!cell.IsCustomRateActive) continue;

                var chargeType = chargeTypes[cell.ChargeTypeId];
                Guard.InvalidField(
                    !RateValuePolicy.IsValid(
                        cell.UnitValue,
                        cell.VatRate,
                        cell.CalculationMethod,
                        chargeType.Behavior),
                    nameof(input.Rows),
                    "Fiyat matrisi değerlerinden biri geçersiz.",
                    "UNIT_PRICING_INVALID_VALUE");
            }
        }

        Guard.InvalidField(
            !submittedPairs.SetEquals(expectedPairs),
            nameof(input.Rows),
            "Fiyat matrisi güncel kategori ve borç tipleriyle eşleşmiyor.",
            "UNIT_PRICING_INCOMPLETE_MATRIX");

        var existingRates = await unitRateRepository.GetForUpdateAsync(input.UnitId);
        foreach (var row in input.Rows)
        {
            foreach (var cell in row.Cells)
            {
                var existingRate = existingRates.FirstOrDefault(rate =>
                    rate.TenantCategoryId == cell.TenantCategoryId
                    && rate.ChargeTypeId == cell.ChargeTypeId);

                if (cell.IsCustomRateActive)
                {
                    if (existingRate == null)
                    {
                        await unitRateRepository.AddAsync(new UnitRate
                        {
                            UnitId = input.UnitId,
                            TenantCategoryId = cell.TenantCategoryId,
                            ChargeTypeId = cell.ChargeTypeId,
                            CalculationMethod = cell.CalculationMethod,
                            UnitValue = cell.UnitValue,
                            KdvRate = cell.VatRate
                        });
                    }
                    else
                    {
                        existingRate.IsDeleted = false;
                        existingRate.IsActive = true;
                        existingRate.CalculationMethod = cell.CalculationMethod;
                        existingRate.UnitValue = cell.UnitValue;
                        existingRate.KdvRate = cell.VatRate;
                    }
                }
                else if (existingRate != null)
                {
                    existingRate.IsDeleted = true;
                    existingRate.IsActive = false;
                }
            }
        }

        foreach (var staleRate in existingRates.Where(rate =>
                     !expectedPairs.Contains((rate.TenantCategoryId, rate.ChargeTypeId))))
        {
            staleRate.IsDeleted = true;
            staleRate.IsActive = false;
        }

        await unitOfWork.SaveChangesAsync();
    }

    private static void EnsureScope(
        int propertyId,
        int unitId,
        UnitPricingAccessScopeInput accessScope)
    {
        if (accessScope.PropertyIds == null && accessScope.UnitIds == null) return;

        var hasPropertyAccess = accessScope.PropertyIds?.Contains(propertyId) == true;
        var hasUnitAccess = accessScope.UnitIds?.Contains(unitId) == true;
        Guard.Forbidden(
            !hasPropertyAccess && !hasUnitAccess,
            "Bu birimin fiyat parametrelerini görüntüleme veya değiştirme yetkiniz bulunmuyor.",
            "UNIT_PRICING_OUT_OF_SCOPE");
    }
}

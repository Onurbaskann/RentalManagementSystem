using System.Collections.Generic;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Models.Dtos;

public record UnitPricingAccessScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetUnitPricingInput(
    int UnitId,
    int Year,
    UnitPricingAccessScopeInput AccessScope);

public record UnitPricingCategoryDto(int Id, string Name);

public record UnitPricingChargeTypeDto(
    int Id,
    string Name,
    string Code,
    ChargeTypeBehavior Behavior);

public record UnitPricingRateDto(
    int Id,
    int TenantCategoryId,
    int ChargeTypeId,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal VatRate);

public record UnitPricingParentRateDto(
    int TenantCategoryId,
    int ChargeTypeId,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal VatRate);

public record UnitPricingContextDto(
    bool UnitExists,
    int UnitId,
    string UnitName,
    int PropertyId,
    string PropertyName,
    UnitTypeUsage UnitTypeUsage,
    string? UnitTypeName,
    IReadOnlyList<UnitPricingCategoryDto> Categories,
    IReadOnlyList<UnitPricingChargeTypeDto> ChargeTypes,
    IReadOnlyList<UnitPricingRateDto> Rates,
    IReadOnlyList<UnitPricingParentRateDto> PropertyRates,
    IReadOnlyList<UnitPricingParentRateDto> GeneralRates);

public record UnitPricingDataDto(
    int UnitId,
    string UnitName,
    int PropertyId,
    string PropertyName,
    bool IsLeasable,
    bool IsReservable,
    string? UnitTypeName,
    List<UnitRateColumn> Columns,
    List<UnitRateCategoryRow> Rows,
    ParentRateCardViewModel? ParentRate,
    ReservationRateOverride? CustomReservationRule,
    ParentReservationRateOverrideCardViewModel? ParentReservationRateOverride
);

public record UnitPricingRowInput(
    int TenantCategoryId,
    IReadOnlyList<UnitPricingCellInput> Cells);

public record UnitPricingCellInput(
    int TenantCategoryId,
    int ChargeTypeId,
    bool IsCustomRateActive,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal VatRate);

public record SaveUnitPricingInput(
    int UnitId,
    IReadOnlyList<UnitPricingRowInput> Rows,
    UnitPricingAccessScopeInput AccessScope);

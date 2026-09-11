using KiraTakip.Models.Enums;
using KiraTakip.Models.Dtos.RateHierarchy;
using KiraTakip.Models.Entities;

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

public class UnitPricingColumnDto
{
    public int ChargeTypeId { get; set; }
    public string ChargeTypeName { get; set; } = string.Empty;
    public string ChargeTypeCode { get; set; } = string.Empty;
    public ChargeTypeBehavior ChargeTypeBehavior { get; set; }
}

public class UnitPricingCategoryRowDto
{
    public int TenantCategoryId { get; set; }
    public string TenantCategoryName { get; set; } = string.Empty;
    public List<UnitPricingCellDto> Cells { get; set; } = [];
}

public class UnitPricingCellDto
{
    public int TenantCategoryId { get; set; }
    public int ChargeTypeId { get; set; }
    public bool IsCustomRateActive { get; set; }
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
    public decimal UnitValue { get; set; }
    public decimal VatRate { get; set; }
    public decimal DefaultUnitValue { get; set; }
    public decimal DefaultVatRate { get; set; }
    public CalculationMethod DefaultCalculationMethod { get; set; } = CalculationMethod.Fixed;
    public string DefaultSource { get; set; } = string.Empty;
}

public record UnitPricingDataDto(
    int UnitId,
    string UnitName,
    int PropertyId,
    string PropertyName,
    bool IsLeasable,
    bool IsReservable,
    string? UnitTypeName,
    List<UnitPricingColumnDto> Columns,
    List<UnitPricingCategoryRowDto> Rows,
    ParentRateCardDto? ParentRate,
    ReservationRateOverride? CustomReservationRule,
    ParentReservationRateOverrideCardDto? ParentReservationRateOverride
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

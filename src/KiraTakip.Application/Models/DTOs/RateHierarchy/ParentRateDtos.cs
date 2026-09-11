using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.RateHierarchy;

public sealed record ParentRateCardDto(
    string SourceName,
    string? Description,
    IReadOnlyList<ParentRateRowDto> Rows);

public sealed record ParentRateRowDto(
    string CategoryName,
    string ChargeTypeName,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal VatRate,
    string? Source = null);

public sealed record ParentReservationRateOverrideCardDto(
    string SourceName,
    string? Description,
    IReadOnlyList<ParentReservationRateOverrideRowDto> Rows);

public sealed record ParentReservationRateOverrideRowDto(
    string UnitTypeName,
    int FreeDurationMinutes,
    int BillingPeriodMinutes,
    decimal PeriodRate,
    decimal VatRate);

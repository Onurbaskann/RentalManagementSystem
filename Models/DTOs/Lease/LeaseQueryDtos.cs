using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos;

public record GetLeasesInput(
    string? Filter = null,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetPagedLeasesInput(
    TableQuery Query,
    string? Filter = null,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetLeaseDetailsInput(int LeaseId);
public record GetTenantLeaseDetailsInput(
    int LeaseId,
    int TenantId,
    LeaseAccessScopeInput AccessScope);
public record GetLeasesByTenantInput(
    int TenantId,
    LeaseAccessScopeInput? AccessScope = null);
public record GetLeasesByUnitInput(int UnitId);
public record GetLeaseDepositsInput(
    IReadOnlyCollection<int> LeaseIds,
    int? TenantId = null);
public record LeaseAccessScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetLeaseSummaryInput(
    int LeaseId,
    int TenantId,
    int UnitId,
    decimal UnitArea,
    DateTime StartDate,
    DateTime EndDate,
    LeaseStatus Status,
    DateTime CurrentTime);

public record LeaseSummaryDto(
    int RemainingDays,
    decimal MonthlyAmount,
    decimal AnnualAmount,
    bool IsActive,
    double DurationPercentage,
    OccupancyStatus UnitStatus);

public record LeaseRateOverrideInput(
    int ChargeTypeId,
    decimal UnitValue,
    CalculationMethod CalculationMethod,
    decimal VatRate);

public record LeaseUnitContextDto(
    int UnitId,
    int PropertyId,
    decimal Area,
    bool IsRentable);

public record CreateLeaseInput(
    int UnitId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    LeaseAccessScopeInput AccessScope);

public record ExtendLeaseInput(
    int LeaseId,
    DateTime NewEndDate,
    bool IsVatApplied,
    decimal VatRate,
    decimal? InflationRate,
    string? Description,
    bool UpdateRate,
    bool CanOverrideRate,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    LeaseAccessScopeInput AccessScope);

public record TerminateLeaseInput(
    int LeaseId,
    DateTime TerminationDate,
    string TerminationReason,
    string? Description,
    LeaseAccessScopeInput AccessScope);

public record UpdateLeaseDueDateInput(
    int LeaseId,
    DueDateRuleType RuleType,
    int DueDay,
    string? Description,
    LeaseAccessScopeInput AccessScope);

public record RegenerateLeaseInput(
    int LeaseId,
    DateTime StartDate,
    bool UpdateRate,
    bool CanOverrideRate,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    LeaseAccessScopeInput AccessScope);

public record GenerateLeaseChargesInput(int LeaseId);
public record RegenerateLeaseChargesInput(int LeaseId, DateTime StartDate);
public record CancelFutureLeaseChargesInput(int LeaseId, DateTime TerminationDate);
public record RecalculateLeaseDueDatesInput(int LeaseId);
public record ComposeLeaseLineItemsInput(
    int UnitId,
    int TenantId,
    DateTime Period,
    int? LeaseId = null,
    LeaseAccessScopeInput? AccessScope = null);

public record CalculateInflationAdjustedAmountInput(decimal CurrentAmount, decimal InflationRate);
public record CalculateVatAmountInput(decimal AmountExcludingVat, decimal VatRate);
public record CalculateVatIncludedAmountInput(decimal AmountExcludingVat, decimal VatRate);
public record CalculateRentIncreaseInput(
    decimal CurrentRentAmount,
    decimal? InflationRate,
    bool ApplyVat,
    decimal? VatRate);

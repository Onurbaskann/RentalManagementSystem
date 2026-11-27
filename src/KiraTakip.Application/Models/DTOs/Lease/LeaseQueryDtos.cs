using KiraTakip.Models.Common;
using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.Lease;

public record GetLeasesInput(
    string? Filter = null,
    SpaceAccessScope? Scope = null);

public record GetPagedLeasesInput(
    TableQuery Query,
    string? Filter = null,
    SpaceAccessScope? Scope = null);

public record GetTerminatedLeaseCountInput(SpaceAccessScope? Scope = null);

public record GetLeaseDetailsInput(int LeaseId);
public record GetTenantLeaseDetailsInput(
    int LeaseId,
    int TenantId,
    SpaceAccessScope AccessScope);
public record GetLeasesByTenantInput(
    int TenantId,
    SpaceAccessScope? AccessScope = null);
public record GetLeasesBySpaceInput(int SpaceId);
public record GetLeaseDepositsInput(
    IReadOnlyCollection<int> LeaseIds,
    int? TenantId = null);
public record GetLeaseSummaryInput(
    int LeaseId,
    int TenantId,
    int UnitId,
    decimal UnitArea,
    DateTime StartDate,
    DateTime EndDate,
    LeaseStatus Status,
    DateTime CurrentTime,
    bool IsRentFree = false);

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

public record LeaseSpaceContextDto(
    int SpaceId,
    string Code,
    string Name,
    string TypeName,
    SpaceUsage Usage,
    decimal Area,
    string Path,
    string NamePath,
    bool IsShared,
    bool IsActive);

public record LeaseSpaceOptionDto(
    int Id,
    string Name,
    string NamePath,
    decimal Area,
    bool IsShared);

public record LeaseOwnerContextDto(int TenantId, IReadOnlyList<string> SpacePaths);

public record CreateLeaseInput(
    int SpaceId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    DueDateRuleType DueDateRuleType,
    int DueDay,
    string? Description,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    SpaceAccessScope AccessScope,
    bool IsRentFree = false,
    decimal? RentedArea = null);

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
    SpaceAccessScope AccessScope);

public record TerminateLeaseInput(
    int LeaseId,
    DateTime TerminationDate,
    string TerminationReason,
    string? Description,
    SpaceAccessScope AccessScope);

public record UpdateLeaseDueDateInput(
    int LeaseId,
    DueDateRuleType RuleType,
    int DueDay,
    string? Description,
    SpaceAccessScope AccessScope);

public record RegenerateLeaseInput(
    int LeaseId,
    DateTime StartDate,
    bool UpdateRate,
    bool CanOverrideRate,
    IReadOnlyCollection<LeaseRateOverrideInput> RateOverrides,
    SpaceAccessScope AccessScope);

public record GenerateLeaseChargesInput(int LeaseId);
public record RegenerateLeaseChargesInput(int LeaseId, DateTime StartDate);
public record CancelFutureLeaseChargesInput(int LeaseId, DateTime TerminationDate);
public record RecalculateLeaseDueDatesInput(int LeaseId);
public record ComposeLeaseLineItemsInput(
    int SpaceId,
    int TenantId,
    DateTime Period,
    int? LeaseId = null,
    SpaceAccessScope? AccessScope = null);

public record CalculateInflationAdjustedAmountInput(decimal CurrentAmount, decimal InflationRate);
public record CalculateVatAmountInput(decimal AmountExcludingVat, decimal VatRate);
public record CalculateVatIncludedAmountInput(decimal AmountExcludingVat, decimal VatRate);
public record CalculateRentIncreaseInput(
    decimal CurrentRentAmount,
    decimal? InflationRate,
    bool ApplyVat,
    decimal? VatRate);

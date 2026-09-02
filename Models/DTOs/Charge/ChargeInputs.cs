using KiraTakip.Models.Entities;
using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos;

public record GetChargesInput(
    int? LeaseId = null,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetChargesPageInput(
    TableQuery Query,
    int? LeaseId = null,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetChargeDetailsInput(int Id);

public record UpdateChargePaidAmountInput(int ChargeId, int ChargeLineItemId);

public record GetChargeIndexOptionsInput(
    bool HasGlobalAccess,
    IReadOnlyList<int>? PropertyIds,
    IReadOnlyList<int>? UnitIds,
    string? Status);

public record ChargePropertyFilterDto(int Id, string Name);

public record ChargeUnitFilterDto(int Id, string Name, int PropertyId);

public record ChargeTenantFilterDto(int Id, string DisplayName);

public record ChargeIndexOptionsDto(
    List<ChargePropertyFilterDto> Properties,
    List<ChargeUnitFilterDto> Units,
    List<ChargeTenantFilterDto> Tenants,
    List<int> AvailableYears,
    int CancelledCount);

public record GetCurrentLeaseChargeInput(int LeaseId, DateTime Today);
public record CurrentLeaseChargeDto(DateTime? Period, List<ChargeLineItem> LineItems);
public record GetTenantLeaseChargeDataInput(
    int TenantId,
    int LeaseId,
    DateTime Today,
    bool IncludeHistory,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);
public record TenantLeaseChargeDataDto(
    List<ChargeListItemDto> Charges,
    CurrentLeaseChargeDto CurrentCharge);
public record GetManualLeaseChargeSummaryInput(int LeaseId);
public record ManualLeaseChargeSummaryDto(int Count, decimal RemainingAmount);

using KiraTakip.Models.Entities;
using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos.Charge;

public record GetChargesInput(
    int? LeaseId = null,
    SpaceAccessScope? Scope = null);

// SpaceId: seçilen alan ve altındaki alanların tahakkukları (Space.Path öneki).
public record GetChargesPageInput(
    TableQuery Query,
    int? LeaseId = null,
    SpaceAccessScope? Scope = null,
    int? SpaceId = null);

public record GetChargeDetailsInput(int Id);

public record UpdateChargePaidAmountInput(int ChargeId, int ChargeLineItemId);

public record GetChargeIndexOptionsInput(
    SpaceAccessScope Scope,
    string? Status);

public record ChargeSpaceOptionDto(int Id, string Name, string NamePath);

public record ChargeTenantFilterDto(int Id, string DisplayName);

public record ChargeIndexOptionsDto(
    List<ChargeSpaceOptionDto> Spaces,
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
    SpaceAccessScope? Scope = null);
public record TenantLeaseChargeDataDto(
    List<ChargeListItemDto> Charges,
    CurrentLeaseChargeDto CurrentCharge);
public record GetManualLeaseChargeSummaryInput(int LeaseId);
public record ManualLeaseChargeSummaryDto(int Count, decimal RemainingAmount);

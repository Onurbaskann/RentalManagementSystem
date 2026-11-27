using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Models.Dtos.Charge;

public record TenantChargeQueryInput(
    int Page,
    int Size,
    string? Search,
    string? Status,
    int? SpaceId,
    string? Source,
    int? Year)
{
    public int Skip => (Page - 1) * Size;
}

public record GetTenantChargeIndexInput(
    int TenantId,
    DateTime Today,
    TenantChargeQueryInput Query,
    SpaceAccessScope Scope);

public record GetTenantChargeDetailsInput(
    int ChargeId,
    int TenantId,
    SpaceAccessScope Scope);

public record TenantChargeOverviewDto(
    decimal TotalChargeAmount,
    decimal RemainingDebtAmount,
    decimal OverdueRemainingAmount,
    List<int> AvailableYears);

public record TenantChargeIndexDataDto(
    PagedResult<ChargeListItemDto> Charges,
    decimal TotalChargeAmount,
    decimal CollectedAmount,
    decimal RemainingDebtAmount,
    decimal OverdueRemainingAmount,
    List<LeaseSpaceOptionDto> Spaces,
    List<int> AvailableYears);

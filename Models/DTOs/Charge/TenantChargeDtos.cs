using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos;

public record TenantChargeQueryInput(
    int Page,
    int Size,
    string? Search,
    string? Status,
    int? UnitId,
    string? Source,
    int? Year)
{
    public int Skip => (Page - 1) * Size;
}

public record GetTenantChargeIndexInput(
    int TenantId,
    DateTime Today,
    TenantChargeQueryInput Query,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetTenantChargeDetailsInput(
    int ChargeId,
    int TenantId,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record TenantChargeOverviewDto(
    decimal TotalChargeAmount,
    decimal RemainingDebtAmount,
    decimal OverdueRemainingAmount,
    List<int> AvailableYears);

public record TenantChargeUnitOptionDto(int Id, string Name);

public record TenantChargeIndexDataDto(
    PagedResult<ChargeListItemDto> Charges,
    decimal TotalChargeAmount,
    decimal CollectedAmount,
    decimal RemainingDebtAmount,
    decimal OverdueRemainingAmount,
    List<TenantChargeUnitOptionDto> Units,
    List<int> AvailableYears);

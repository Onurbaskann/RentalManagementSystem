using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos;

public record GetTenantsInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetPagedTenantsInput(
    TableQuery Query,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record TenantAccessScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetTenantDetailsInput(
    int TenantId,
    TenantAccessScopeInput? AccessScope = null);

public record GetTenantProfileInput(int TenantId);

public record CreateTenantInput(
    string TenantNo,
    string Name,
    string? TradeRegistryNo,
    string? TaxNo,
    string? TaxOffice,
    string? MersisNo,
    string Phone,
    string Email,
    string? Address,
    int? TenantCategoryId,
    int? SectorId,
    IReadOnlyList<TenantDocumentUploadInput> Documents,
    TenantAccessScopeInput AccessScope);

public record TenantDocumentUploadInput(
    int DocumentTypeId,
    string FileName,
    string MimeType,
    byte[] Content);

public record UpdateTenantInput(
    int TenantId,
    string TenantNo,
    string Name,
    string? TradeRegistryNo,
    string? TaxNo,
    string? TaxOffice,
    string? MersisNo,
    string Phone,
    string Email,
    string? Address,
    int? TenantCategoryId,
    int? SectorId,
    TenantAccessScopeInput AccessScope);

public record CreatedTenantDto(int TenantId, string DisplayName);

public record CheckTenantInactiveInput(int TenantId);

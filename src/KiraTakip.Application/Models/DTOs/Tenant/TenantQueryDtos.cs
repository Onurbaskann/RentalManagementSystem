using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos.Tenant;

public record GetTenantsInput(SpaceAccessScope? Scope = null);

public record GetPagedTenantsInput(
    TableQuery Query,
    SpaceAccessScope? Scope = null);

public record GetTenantDetailsInput(
    int TenantId,
    SpaceAccessScope? AccessScope = null);

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
    int TenantCategoryId,
    int? SectorId,
    IReadOnlyList<TenantDocumentUploadInput> Documents,
    SpaceAccessScope AccessScope);

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
    int TenantCategoryId,
    int? SectorId,
    SpaceAccessScope AccessScope);

public record CreatedTenantDto(int TenantId, string DisplayName);

public record CheckTenantInactiveInput(int TenantId);

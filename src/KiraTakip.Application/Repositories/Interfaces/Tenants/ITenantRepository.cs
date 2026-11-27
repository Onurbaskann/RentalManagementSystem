using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Reservation;
using KiraTakip.Models.Dtos.Document;

namespace KiraTakip.Repositories.Interfaces.Tenants;

public interface ITenantRepository : IRepositoryBase<Tenant>
{
    Task<List<TenantListItemDto>> GetListAsync(
        SpaceAccessScope? scope);
    Task<PagedResult<TenantListItemDto>> GetPagedListAsync(
        TableQuery query,
        SpaceAccessScope? scope);
    Task<bool> IsInScopeAsync(
        int tenantId,
        SpaceAccessScope? scope);
    Task<List<ReservationTenantOptionDto>> GetReservationOptionsAsync();
    Task<TenantDetailsDto?> GetDetailsAsync(
        int id,
        SpaceAccessScope? scope = null);
    Task<Tenant?> GetForUpdateAsync(
        int id,
        SpaceAccessScope? scope = null);
    Task<bool> TenantNoExistsAsync(string tenantNo, int? excludeTenantId = null);
    Task<bool> TaxNoExistsAsync(string taxNo, int? excludeTenantId = null);
    Task<List<string>> GetExistingTenantNosAsync();
    Task<int?> GetCategoryIdAsync(int tenantId);
    Task<bool> IsInactiveAsync(int tenantId, CancellationToken ct = default);
    Task<Tenant?> GetByIdIgnoreQueryFiltersAsync(int id, CancellationToken ct = default);
    Task<Tenant?> GetActiveByIdAsync(int id, CancellationToken ct = default);
    Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int tenantId);

    /// <summary>
    /// Kiracının sözleşme/tahakkuk/rezervasyon/kullanıcı geçmişi var mı — varsa silinemez
    /// (UnitRepository.HasHistoricalDependencyAsync ile aynı desen).
    /// </summary>
    Task<bool> HasHistoricalDependencyAsync(int tenantId);
}

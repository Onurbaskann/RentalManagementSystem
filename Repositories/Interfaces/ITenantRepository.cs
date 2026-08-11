using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;

namespace KiraTakip.Repositories.Interfaces;

public interface ITenantRepository : IRepositoryBase<Tenant>
{
    Task<List<TenantListItemDto>> GetListAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PagedResult<TenantListItemDto>> GetPagedListAsync(
        TableQuery query,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<bool> IsInScopeAsync(
        int tenantId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<List<ReservationTenantOptionDto>> GetReservationOptionsAsync();
    Task<TenantDetailsDto?> GetDetailsAsync(
        int id,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null);
    Task<Tenant?> GetForUpdateAsync(
        int id,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null);
    Task<bool> TenantNoExistsAsync(string tenantNo, int? excludeTenantId = null);
    Task<bool> TaxNoExistsAsync(string taxNo, int? excludeTenantId = null);
    Task<List<string>> GetExistingTenantNosAsync();
    Task<int?> GetCategoryIdAsync(int tenantId);
    Task<bool> IsInactiveAsync(int tenantId, CancellationToken ct = default);
    Task<Tenant?> GetByIdIgnoreQueryFiltersAsync(int id, CancellationToken ct = default);
    Task<Tenant?> GetActiveByIdAsync(int id, CancellationToken ct = default);
    Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int tenantId);
}

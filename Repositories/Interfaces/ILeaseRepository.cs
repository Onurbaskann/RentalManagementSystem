using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;

namespace KiraTakip.Repositories.Interfaces;

public interface ILeaseRepository : IRepositoryBase<Lease>
{
    Task<List<LeaseListItemDto>> GetListAsync(
        string? filter,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<PagedResult<LeaseListItemDto>> GetPagedListAsync(
        TableQuery query,
        string? filter,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<LeaseDetailDto?> GetDetailsAsync(int id);
    Task<LeaseDetailDto?> GetTenantDetailsAsync(int leaseId, int tenantId, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);
    Task<List<LeaseListItemDto>> GetTenantPortalListAsync(
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null);
    Task<PagedResult<LeaseListItemDto>> GetTenantPortalPagedListAsync(
        int tenantId,
        TableQuery query,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null);
    Task<List<LeaseListItemDto>> GetByTenantIdAsync(
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null);
    Task<List<LeaseListItemDto>> GetByUnitIdAsync(int unitId);
    Task<int> CountActiveByTenantAsync(int tenantId, DateTime currentTime, List<int>? authorizedPropertyIds = null, List<int>? authorizedUnitIds = null);

    // Dropdown — entity döner (Tenant + Unit + Property yüklü)
    Task<List<Lease>> GetAktiflerAsync();

    // Dropdown — DTO döner (Manuel Borç ekleme ekranı)
    Task<List<LeaseDropdownDto>> GetActiveDropdownAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);

    // RateResolver için projeksiyon: TasinmazId + KiraciKategoriId
    Task<(int TasinmazId, int? KategoriId)?> GetPropertyAndCategoryAsync(int leaseId);

    Task<List<UnitLookupDto>> GetActiveLeaseUnitsByTenantIdAsync(int tenantId, CancellationToken ct = default);
    Task<bool> HasActiveLeaseForUnitAsync(int unitId, DateTime currentTime);
    Task<LeaseDraftEditDto?> GetDraftForEditAsync(
        int leaseId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<Lease?> GetForDecisionAsync(
        int leaseId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<bool> HasOpenApplicationForUnitAsync(int unitId, int? excludedLeaseId = null);
    Task<bool> HasChargesAsync(int leaseId);
    Task<bool> HasCreationActivityAsync(int leaseId);
    Task<Lease?> GetDeletedApplicationForAuditAsync(int leaseId);
    Task<Lease?> GetWithActivityLogAsync(int leaseId);
    Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int leaseId, bool tenantPortalOnly = false);
}

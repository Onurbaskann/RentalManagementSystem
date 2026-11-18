using KiraTakip.Models.Enums;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Document;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Domain.Leases;

namespace KiraTakip.Repositories.Interfaces.Leases;

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
    Task<int> GetTerminatedCountAsync(List<int>? authorizedPropertyIds, List<int>? authorizedUnitIds = null);

    // Dropdown — entity döner (Tenant + Unit + Property yüklü)
    Task<List<Lease>> GetAktiflerAsync();

    // Dropdown — DTO döner (Manuel Borç ekleme ekranı)
    Task<List<LeaseDropdownDto>> GetActiveDropdownAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);

    // RateResolver için projeksiyon: TasinmazId + KiraciKategoriId
    Task<(int TasinmazId, int? KategoriId)?> GetPropertyAndCategoryAsync(int leaseId);
    // Toplu (bulk) hali — GetPropertyAndCategoryAsync ile aynı, IN(...) ile genişletilmiş.
    Task<Dictionary<int, (int PropertyId, int? TenantCategoryId)>> GetPropertyAndCategoriesAsync(
        IReadOnlyCollection<int> leaseIds);

    Task<List<UnitLookupDto>> GetActiveLeaseUnitsByTenantIdAsync(
        int tenantId,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null,
        CancellationToken ct = default);
    Task<bool> HasOverlappingActiveLeaseAsync(
        int unitId,
        DateTime startDate,
        DateTime endDate,
        int? excludeLeaseId,
        int? excludeLeaseGroupId = null);
    Task<LeaseDraftEditDto?> GetDraftForEditAsync(
        int leaseId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<Lease?> GetForDecisionAsync(
        int leaseId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);
    Task<bool> HasOpenApplicationForUnitAsync(
        int unitId,
        int? excludedLeaseId = null,
        int? excludedLeaseGroupId = null);
    Task<bool> HasChargesAsync(int leaseId);
    /// <summary>İptal edilmemiş ve Paid olmayan tahakkuk var mı.</summary>
    Task<bool> HasUnsettledChargesAsync(int leaseId);
    Task<bool> HasCreationActivityAsync(int leaseId);
    Task<Lease?> GetDeletedApplicationForAuditAsync(int leaseId);
    Task<Lease?> GetWithActivityLogAsync(int leaseId);
    Task<List<int>> GetExpiredActiveLeaseIdsAsync(DateTime today, int batchSize);
    Task<Lease?> GetForExpirationAsync(int leaseId);
    Task<DocumentOwnerContextDto?> GetDocumentOwnerContextAsync(int leaseId, bool tenantPortalOnly = false);
    Task<List<string>> GetExistingLeaseNosAsync();
    Task<decimal?> GetRentedAreaAsync(int leaseId);
    Task<Dictionary<int, decimal>> GetRentedAreasAsync(IReadOnlyCollection<int> leaseIds);
    Task<IReadOnlyList<CapacityOccupant>> GetOpenLeaseAreasForUnitAsync(
        int unitId,
        DateTime start,
        DateTime end,
        int? excludeLeaseId);
    Task<List<CapacityPlanLeaseDto>> GetOpenLeasesForCapacityPlanAsync(int unitId, DateTime fromDate);
    Task<List<LeaseArea>> GetTrackedLeaseAreasAsync(IReadOnlyCollection<int> leaseIds);
    Task<LeaseArea?> GetLeaseAreaAsync(int leaseId);
    Task<LeaseArea> UpsertLeaseAreaAsync(int leaseId, decimal rentedArea, decimal allocatedArea, LeaseAreaMode mode);
    void AddLeaseAreaHistory(LeaseArea area, LeaseAreaMode mode, DateTime effectiveFrom, LeaseAreaChangeReason reason);
    Task<IReadOnlyList<LeaseAreaHistory>> GetLeaseAreaHistoryAsync(int leaseId);
    Task<IReadOnlyList<LeaseAreaHistoryItemDto>> GetLeaseAreaHistoryItemsAsync(int leaseId);
    Task RemoveLeaseAreaAsync(int leaseId);
}

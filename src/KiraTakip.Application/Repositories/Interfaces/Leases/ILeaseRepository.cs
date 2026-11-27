using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Document;

namespace KiraTakip.Repositories.Interfaces.Leases;

public interface ILeaseRepository : IRepositoryBase<Lease>
{
    Task<List<LeaseListItemDto>> GetListAsync(string? filter, SpaceAccessScope? scope);
    Task<PagedResult<LeaseListItemDto>> GetPagedListAsync(
        TableQuery query,
        string? filter,
        SpaceAccessScope? scope);
    Task<LeaseDetailDto?> GetDetailsAsync(int id);
    Task<LeaseDetailDto?> GetTenantDetailsAsync(int leaseId, int tenantId, SpaceAccessScope? scope = null);
    Task<List<LeaseListItemDto>> GetTenantPortalListAsync(int tenantId, SpaceAccessScope? scope = null);
    Task<PagedResult<LeaseListItemDto>> GetTenantPortalPagedListAsync(
        int tenantId,
        TableQuery query,
        SpaceAccessScope? scope = null);
    Task<List<LeaseListItemDto>> GetByTenantIdAsync(int tenantId, SpaceAccessScope? scope = null);
    Task<List<LeaseListItemDto>> GetBySpaceIdAsync(int spaceId);
    Task<int> CountActiveByTenantAsync(int tenantId, DateTime currentTime, SpaceAccessScope? scope = null);
    Task<int> GetTerminatedCountAsync(SpaceAccessScope? scope);

    // Dropdown — entity döner (Tenant + Areas.Space yüklü)
    Task<List<Lease>> GetAktiflerAsync();

    // Dropdown — DTO döner (Manuel Borç ekleme ekranı)
    Task<List<LeaseDropdownDto>> GetActiveDropdownAsync(SpaceAccessScope? scope);

    // Tarife çözümü için projeksiyon: sözleşmenin (tek) alanı + yolu + kiracı kategorisi.
    Task<(int SpaceId, string SpacePath, int TenantCategoryId)?> GetSpaceAndCategoryAsync(int leaseId);
    // Toplu (bulk) hali — GetSpaceAndCategoryAsync ile aynı, IN(...) ile genişletilmiş.
    Task<Dictionary<int, (int SpaceId, string SpacePath, int TenantCategoryId)>> GetSpaceAndCategoriesAsync(
        IReadOnlyCollection<int> leaseIds);

    /// <summary>Kiracının aktif sözleşmelerindeki alanlar; ad alanı = sözleşmedeki anlık yol.</summary>
    Task<List<LeaseSpaceOptionDto>> GetActiveLeaseSpacesByTenantIdAsync(
        int tenantId,
        SpaceAccessScope? scope = null,
        CancellationToken ct = default);

    /// <summary>Kiracı, kapsamdaki alanlarda en az bir sözleşmeye sahipse true. Kapsam yoksa/global ise true.</summary>
    Task<bool> IsTenantInScopeAsync(int tenantId, SpaceAccessScope? scope);

    Task<LeaseSpaceContextDto?> GetSpaceContextAsync(int spaceId);

    /// <summary>
    /// Aktif ve kiralanabilir alanların düz listesi (kapsamdakiler). sharedOnly: true = yalnız paylaşımlı,
    /// false = paylaşımlı olmayanlar. includeSpaceId verilirse (taslak düzenleme) o alan her durumda listeye girer.
    /// </summary>
    Task<List<LeaseSpaceOptionDto>> GetSelectableSpacesAsync(
        bool sharedOnly,
        SpaceAccessScope? scope,
        int? includeSpaceId = null);

    /// <summary>Çakışma yalnız aynı alan için (D2).</summary>
    Task<bool> HasOverlappingActiveLeaseAsync(
        int spaceId,
        DateTime startDate,
        DateTime endDate,
        int? excludeLeaseId,
        int? excludeLeaseGroupId = null);
    Task<LeaseDraftEditDto?> GetDraftForEditAsync(int leaseId, SpaceAccessScope? scope);
    /// <summary>İzlenen taslak; alan satırları (Areas) yüklü. Kapsam dışıysa null.</summary>
    Task<Lease?> GetForDecisionAsync(int leaseId, SpaceAccessScope? scope);
    Task<bool> HasOpenApplicationForSpaceAsync(
        int spaceId,
        int? excludedLeaseId = null,
        int? excludedLeaseGroupId = null);
    Task<bool> HasChargesAsync(int leaseId);
    /// <summary>İptal edilmemiş ve Paid olmayan tahakkuk var mı.</summary>
    Task<bool> HasUnsettledChargesAsync(int leaseId);
    Task<bool> HasCreationActivityAsync(int leaseId);
    Task<Lease?> GetDeletedApplicationForAuditAsync(int leaseId);
    /// <summary>İşlem günlüğü ve alan satırları (Areas.Space) yüklü.</summary>
    Task<Lease?> GetWithActivityLogAsync(int leaseId);
    Task<List<int>> GetExpiredActiveLeaseIdsAsync(DateTime today, int batchSize);
    Task<Lease?> GetForExpirationAsync(int leaseId);
    Task<LeaseOwnerContextDto?> GetDocumentOwnerContextAsync(int leaseId, bool tenantPortalOnly = false);
    Task<List<string>> GetExistingLeaseNosAsync();
    Task<decimal?> GetRentedAreaAsync(int leaseId);
    Task<Dictionary<int, decimal>> GetRentedAreasAsync(IReadOnlyCollection<int> leaseIds);

    /// <summary>
    /// Sözleşmenin alan satırlarını verilen listeyle eşitler: yeni alan eklenir, aynı alan güncellenir,
    /// listede olmayan satır silinir. Anlık alanlar alanın o anki değerlerinden yazılır.
    /// </summary>
    Task ReplaceLeaseAreasAsync(
        int leaseId,
        IReadOnlyCollection<(LeaseSpaceContextDto Space, decimal RentedArea)> areas);
}

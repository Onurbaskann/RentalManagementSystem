using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Spaces;

public interface ISpaceRateRepository : IRepositoryBase<SpaceRate>
{
    Task<List<SpaceRate>> GetBySpaceAsync(int spaceId);
    Task<List<(int SpaceId, KiraTakip.Models.Dtos.RateValueDto Rate)>> GetActiveRatesAsync(
        IReadOnlyCollection<int> spaceIds, int tenantCategoryId, int chargeTypeId);
    // Toplu hali: kiracı kategorisi ve borç tipi kümeleri için tek sorgu (BatchRateResolver).
    Task<List<(int SpaceId, int TenantCategoryId, int ChargeTypeId, KiraTakip.Models.Dtos.RateValueDto Rate)>> GetActiveRatesAsync(
        IReadOnlyCollection<int> spaceIds, IReadOnlyCollection<int> tenantCategoryIds, IReadOnlyCollection<int> chargeTypeIds);
    // "Üstten gelen değer" kartı için: verilen alanların aktif tarife satırları (kategori filtresi isteğe bağlı).
    Task<List<(int SpaceId, KiraTakip.Models.Dtos.RateHierarchy.ParentRateRowDto Row)>> GetParentRowsAsync(
        IReadOnlyCollection<int> spaceIds, int? tenantCategoryId);
    Task<SpaceRate?> GetByKeyAsync(int spaceId, int tenantCategoryId, int chargeTypeId);
}

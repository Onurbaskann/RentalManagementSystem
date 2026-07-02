using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IUnitTypeRepository : IBaseRepository<UnitType>
{
    Task<List<UnitTypeListItemDto>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);

    // Cross-aggregate kontroller (BorcTipi DurumDegistir + UnitType DurumDegistir için)
    Task<bool> AnyAktifByBorcTipiIdAsync(int borcTipiId, int? excludeId = null);
    Task<bool> HasAktifTahakkukForUnitTypeAsync(int birimTuruId);
    Task<bool> HasPlanlanmisRezervasyonForUnitTypeAsync(int birimTuruId);
}

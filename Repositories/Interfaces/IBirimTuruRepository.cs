using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IBirimTuruRepository : IBaseRepository<BirimTuru>
{
    Task<List<BirimTuruListItemDto>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);

    // Cross-aggregate kontroller (BorcTipi DurumDegistir + BirimTuru DurumDegistir için)
    Task<bool> AnyAktifByBorcTipiIdAsync(int borcTipiId, int? excludeId = null);
    Task<bool> HasAktifTahakkukForBirimTuruAsync(int birimTuruId);
    Task<bool> HasPlanlanmisRezervasyonForBirimTuruAsync(int birimTuruId);
}

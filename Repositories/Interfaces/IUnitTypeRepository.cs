using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IUnitTypeRepository : IBaseRepository<UnitType>
{
    Task<List<UnitTypeListItemDto>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);

    // Cross-aggregate kontroller (ChargeType DurumDegistir + UnitType DurumDegistir için)
    Task<bool> AnyAktifByBorcTipiIdAsync(int chargeTypeId, int? excludeId = null);
    Task<bool> HasAktifTahakkukForUnitTypeAsync(int unitTypeId);
    Task<bool> HasPlanlanmisRezervasyonForUnitTypeAsync(int unitTypeId);
}

using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IBorcTipiRepository : IBaseRepository<ChargeType>
{
    Task<List<BorcTipiLookupDto>> GetManuelBorcTipleriAsync();
    Task<ChargeType?> GetActiveManuelByIdAsync(int id);
    Task<List<BorcTipiListItemDto>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
    Task<List<BorcTipiLookupDto>> GetRezervasyonAdaylariAsync();
}

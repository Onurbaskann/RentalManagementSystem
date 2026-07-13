using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ITasinmazTipiRepository : IBaseRepository<PropertyType>
{
    Task<List<TasinmazTipiListItemDto>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
}

namespace KiraTakip.Repositories.Interfaces;

public interface IBelgeTuruRepository : IBaseRepository<BelgeTuru>
{
    Task<List<BelgeTuru>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
}

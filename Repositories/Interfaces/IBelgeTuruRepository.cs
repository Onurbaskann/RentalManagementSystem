namespace KiraTakip.Repositories.Interfaces;

public interface IBelgeTuruRepository : IBaseRepository<DocumentType>
{
    Task<List<DocumentType>> GetListAsync();
    Task<int> GetMaxSiraAsync();
    Task<bool> KodExistsAsync(string kod, int? excludeId = null);
}

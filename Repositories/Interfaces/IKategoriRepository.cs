using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IKategoriRepository : IBaseRepository<Kategori>
{
    Task<List<KategoriListItemDto>> GetListByTipiAsync(KategoriTipi tipi);
    Task<Kategori?> GetByIdAndTipiAsync(int id, KategoriTipi tipi);
    Task<int> GetMaxSiraByTipiAsync(KategoriTipi tipi);
    Task<bool> KodExistsByTipiAsync(KategoriTipi tipi, string kod, int? excludeId = null);
}

using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ITasinmazRepository : IBaseRepository<Tasinmaz>
{
    Task<List<TasinmazListItemDto>> GetListAsync(List<int>? yetkiliTasinmazIds);
    Task<TasinmazDetayDto?> GetDetayAsync(int id);
    Task<List<BirimLookupDto>> GetBosBirimlerAsync(List<int>? yetkiliTasinmazIds);
    Task AddRezervasyonTarifeAsync(RezervasyonTarife tarife);
    Task<Tasinmaz?> GetWithBirimlerTrackedAsync(int id);
}

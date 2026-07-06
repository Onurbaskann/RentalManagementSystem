using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IPropertyRepository : IBaseRepository<Property>
{
    Task<List<TasinmazListItemDto>> GetListAsync(List<int>? yetkiliPropertyIds);
    Task<TasinmazDetayDto?> GetDetayAsync(int id);
    Task<List<BirimLookupDto>> GetBosBirimlerAsync(List<int>? yetkiliPropertyIds);
    Task<List<BirimLookupDto>> GetTumBirimlerAsync(List<int>? yetkiliPropertyIds);
    Task AddRezervasyonTarifeAsync(RezervasyonTarife tarife);
    Task<Property?> GetWithBirimlerTrackedAsync(int id);
}

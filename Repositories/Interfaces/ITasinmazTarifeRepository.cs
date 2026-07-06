using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ITasinmazTarifeRepository : IBaseRepository<TasinmazTarife>
{
    Task<List<TasinmazTarife>> GetByTasinmazIdAsync(int tasinmazId);
    Task<List<Kategori>> GetKiraciKategorileriAsync();
    Task<List<ChargeType>> GetBorcTipleriMatrisIcinAsync();
    Task<List<TasinmazTarife>> GetForHiyerarsiAsync(int tasinmazId, int? kategoriId);
    Task<RateValueDto?> GetRateAsync(int tasinmazId, int kategoriId, int chargeTypeId);
}

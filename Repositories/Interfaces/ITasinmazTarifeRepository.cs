using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ITasinmazTarifeRepository : IBaseRepository<TasinmazTarife>
{
    Task<List<TasinmazTarife>> GetByPropertyIdAsync(int propertyId);
    Task<List<Kategori>> GetKiraciKategorileriAsync();
    Task<List<ChargeType>> GetBorcTipleriMatrisIcinAsync();
    Task<List<TasinmazTarife>> GetForHiyerarsiAsync(int propertyId, int? kategoriId);
    Task<RateValueDto?> GetRateAsync(int propertyId, int kategoriId, int chargeTypeId);
}

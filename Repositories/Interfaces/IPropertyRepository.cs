using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IPropertyRepository : IBaseRepository<Property>
{
    Task<List<TasinmazListItemDto>> GetListAsync(List<int>? yetkiliPropertyIds);
    Task<PropertyDetailDto?> GetDetayAsync(int id);
    Task<List<UnitLookupDto>> GetBosBirimlerAsync(List<int>? yetkiliPropertyIds);
    Task<List<UnitLookupDto>> GetTumBirimlerAsync(List<int>? yetkiliPropertyIds);
    Task AddReservationRateOverrideAsync(ReservationRateOverride tarife);
    Task<Property?> GetWithBirimlerTrackedAsync(int id);
}

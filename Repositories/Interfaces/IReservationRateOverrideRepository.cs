using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IReservationRateOverrideRepository : IBaseRepository<ReservationRateOverride>
{
    Task<List<ParentReservationRateOverrideRow>> GetGeneralRowsAsync(int year);
    Task<List<ReservationRateOverrideListItemDto>> GetUcretKurallariListAsync();
    Task<ReservationRateOverride?> GetActiveForUnitAsync(int unitId);
    Task<ReservationRateOverride?> GetForUnitAsync(int unitId);
    Task<ReservationRateOverride?> GetGeneralAsync(int unitTypeId, int year);
    Task<ReservationRateOverride?> GetWithUnitAsync(int id);
    Task<Dictionary<int, ReservationRateOverride>> GetByUnitIdsAsync(IReadOnlyCollection<int> unitIds, bool activeOnly);
    void Remove(ReservationRateOverride rate);
}

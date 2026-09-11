using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.RateHierarchy;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Reservations;

public interface IReservationRateOverrideRepository : IRepositoryBase<ReservationRateOverride>
{
    Task<List<ParentReservationRateOverrideRowDto>> GetGeneralRowsAsync(int year);
    Task<List<ReservationRateOverrideListItemDto>> GetUcretKurallariListAsync();
    Task<PagedResult<ReservationRateOverrideListItemDto>> GetRateRulesPagedAsync(TableQuery query);
    Task<ReservationRateOverride?> GetActiveForUnitAsync(int unitId);
    Task<ReservationRateOverride?> GetForUnitAsync(int unitId);
    Task<ReservationRateOverride?> GetGeneralAsync(int unitTypeId, int year);
    Task<ReservationRateOverride?> GetWithUnitAsync(int id);
    Task<Dictionary<int, ReservationRateOverride>> GetByUnitIdsAsync(IReadOnlyCollection<int> unitIds, bool activeOnly);
    void Remove(ReservationRateOverride rate);
}

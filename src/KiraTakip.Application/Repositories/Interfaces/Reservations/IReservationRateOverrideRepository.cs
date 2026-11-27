using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Reservations;

public interface IReservationRateOverrideRepository : IRepositoryBase<ReservationRateOverride>
{
    Task<PagedResult<ReservationRateOverrideListItemDto>> GetRateRulesPagedAsync(TableQuery query, SpaceAccessScope scope);

    /// <summary>
    /// Alanın kendi aktif kuralı, yoksa en yakın atanın aktif kuralı (D8). Alan ve ata zinciri tek sorguda gelir.
    /// <paramref name="includeSelf"/> false ise yalnız ataların kuralı aranır.
    /// </summary>
    Task<ReservationRateOverride?> GetActiveForSpaceAsync(int spaceId, string spacePath, bool includeSelf = true);

    Task<ReservationRateOverride?> GetInheritedForSpaceAsync(int spaceId);
    Task<ReservationRateOverride?> GetForSpaceAsync(int spaceId);
    Task<ReservationRateOverride?> GetWithSpaceAsync(int id);
    Task<bool> AnyActiveByChargeTypeAsync(int chargeTypeId);
    void Remove(ReservationRateOverride rate);
}

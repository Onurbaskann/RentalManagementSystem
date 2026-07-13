using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IReservationRateOverrideRepository : IBaseRepository<ReservationRateOverride>
{
    Task<List<ParentReservationRateOverrideRow>> GetGenelForKartAsync(int year);
    Task<List<ReservationRateOverrideListItemDto>> GetUcretKurallariListAsync();
}

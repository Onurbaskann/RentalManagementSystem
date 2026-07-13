using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IRateScheduleRepository : IBaseRepository<RateSchedule>
{
    Task<RateValueDto?> GetRateAsync(int kategoriId, int chargeTypeId, int donemYil);
    Task<List<ParentTarifeSatir>> GetByYilKategoriForKartAsync(int yil, int? kategoriId);
}

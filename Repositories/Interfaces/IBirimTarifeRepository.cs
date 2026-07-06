using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IBirimTarifeRepository : IBaseRepository<BirimTarife>
{
    Task<RateValueDto?> GetRateAsync(int unitId, int kategoriId, int chargeTypeId);
    Task<List<ParentTarifeKartViewModel>> GetByBirimForKartAsync(int unitId, int? kategoriId);
}

using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IGenelTarifeRepository : IBaseRepository<GenelTarife>
{
    Task<RateValueDto?> GetRateAsync(int kategoriId, int chargeTypeId, int donemYil);
    Task<List<ParentTarifeSatir>> GetByYilKategoriForKartAsync(int yil, int? kategoriId);
}

using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IBirimTarifeRepository : IBaseRepository<BirimTarife>
{
    Task<RateValueDto?> GetRateAsync(int birimId, int kategoriId, int borcTipiId);
    Task<List<ParentTarifeKartViewModel>> GetByBirimForKartAsync(int birimId, int? kategoriId);
}

using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Repositories.Interfaces;

public interface IRezervasyonTarifeRepository : IBaseRepository<RezervasyonTarife>
{
    Task<List<ParentRezervasyonTarifeSatir>> GetGenelForKartAsync(int yil);
    Task<List<RezervasyonTarifeKuralListItemDto>> GetUcretKurallariListAsync();
}

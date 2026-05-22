using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IKiraciRepository : IBaseRepository<Kiraci>
{
    Task<List<KiraciListItemDto>> GetListAsync(List<int>? yetkiliTasinmazIds);
    Task<KiraciDetayDto?> GetDetayAsync(int id);
    Task<List<string>> GetExistingKiraciNosAsync();
    Task<int?> GetKategoriIdAsync(int kiraciId);
}

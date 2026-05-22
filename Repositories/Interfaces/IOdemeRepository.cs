using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IOdemeRepository : IBaseRepository<KiraOdeme>
{
    Task<List<OdemeListItemDto>> GetListAsync(int? tahakkukId, List<int>? yetkiliTasinmazIds);
    Task<PagedResult<OdemeListItemDto>> GetPagedListAsync(TableQuery q, int? tahakkukId, List<int>? yetkiliTasinmazIds);
    Task<OdemeDetayDto?> GetDetayAsync(int id);
}

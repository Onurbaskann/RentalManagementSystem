using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ITahakkukService
{
    // Listeleme — DTO döner
    Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId = null, IReadOnlyList<int>? tasinmazIds = null, IReadOnlyList<int>? birimIds = null);
    Task<PagedResult<TahakkukListItemDto>> GetPagedAsync(TableQuery q, int? sozlesmeId = null, IReadOnlyList<int>? tasinmazIds = null, IReadOnlyList<int>? birimIds = null);
    Task<TahakkukDetayDto?> GetDetayAsync(int id);

    // Business operations
    Task GecikmeleriGuncelleAsync();
    Task OdenenTutarGuncelleAsync(int tahakkukId);
}

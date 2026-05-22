using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ITahakkukRepository : IBaseRepository<KiraTahakkuk>
{
    // Okuma — DTO döner
    Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds);
    Task<PagedResult<TahakkukListItemDto>> GetPagedListAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds);
    Task<TahakkukDetayDto?> GetDetayAsync(int id);

    // Manuel Borç — DTO döner
    Task<List<ManuelBorcListItemDto>> GetManuelBorcListAsync(List<int>? yetkiliTasinmazIds);

    // Business logic — entity döner (tracked)
    Task<List<KiraTahakkuk>> GetGeciktirileceklerAsync(DateTime bugun);
    Task<KiraTahakkuk?> GetManuelBorcByIdAsync(int id);
    Task<List<KiraTahakkuk>> GetBekleyenBorclarAsync(DateTime limitVade, CancellationToken ct);

    // Hesaplama
    Task<decimal> GetOdenenTutarAsync(int tahakkukId);

    // Üretim yardımcıları (TahakkukUretimService için)
    Task<List<BorcTipi>> GetAktifUretimBorcTipleriAsync();
    Task<List<KiraTahakkuk>> GetSilineceklerAsync(int sozlesmeId, DateTime ilkGun);
    Task DeleteRangeAsync(IEnumerable<KiraTahakkuk> entities);
}

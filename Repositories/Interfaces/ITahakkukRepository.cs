using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface ITahakkukRepository : IBaseRepository<Tahakkuk>
{
    // Okuma — DTO döner
    Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null);
    Task<PagedResult<TahakkukListItemDto>> GetPagedListAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null);
    Task<TahakkukDetayDto?> GetDetayAsync(int id);

    // Manuel Borç — DTO döner
    Task<List<ManuelBorcListItemDto>> GetManuelBorcListAsync(List<int>? yetkiliTasinmazIds, string? durum = null, string? baglanti = null, int? sozlesmeId = null, List<int>? yetkiliBirimIds = null);
    Task<int> GetManuelBorcIptalSayisiAsync(List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null);

    // Business logic — entity döner (tracked)
    Task<List<Tahakkuk>> GetGeciktirileceklerAsync(DateTime bugun);
    Task<Tahakkuk?> GetManuelBorcByIdAsync(int id);
    Task<List<Tahakkuk>> GetBekleyenBorclarAsync(DateTime limitVade, CancellationToken ct);

    // Hesaplama
    Task<decimal> GetOdenenTutarAsync(int tahakkukId);

    // Üretim yardımcıları (TahakkukUretimService için)
    Task<List<BorcTipi>> GetAktifUretimBorcTipleriAsync();
    Task<List<Tahakkuk>> GetSilineceklerAsync(int sozlesmeId, DateTime ilkGun);
    Task DeleteRangeAsync(IEnumerable<Tahakkuk> entities);
}

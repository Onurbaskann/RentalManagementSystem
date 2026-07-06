using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IChargeRepository : IBaseRepository<Charge>
{
    // Okuma — DTO döner
    Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null);
    Task<PagedResult<TahakkukListItemDto>> GetPagedListAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null);
    Task<TahakkukDetayDto?> GetDetayAsync(int id);

    // Manuel Borç — DTO döner
    Task<List<ManuelBorcListItemDto>> GetManuelBorcListAsync(List<int>? yetkiliTasinmazIds, string? durum = null, string? baglanti = null, int? sozlesmeId = null, List<int>? yetkiliBirimIds = null);
    Task<int> GetManuelBorcIptalSayisiAsync(List<int>? yetkiliTasinmazIds, List<int>? yetkiliBirimIds = null);

    // Business logic — entity döner (tracked)
    Task<List<Charge>> GetGeciktirileceklerAsync(DateTime bugun);
    Task<Charge?> GetManuelBorcByIdAsync(int id);
    Task<List<Charge>> GetBekleyenBorclarAsync(DateTime limitVade, CancellationToken ct);

    // Hesaplama
    Task<decimal> GetOdenenTutarAsync(int tahakkukId);

    // Üretim yardımcıları (ChargeGenerationService için)
    Task<List<ChargeType>> GetAktifUretimBorcTipleriAsync();
    Task<List<Charge>> GetSilineceklerAsync(int sozlesmeId, DateTime ilkGun);
    Task DeleteRangeAsync(IEnumerable<Charge> entities);
}

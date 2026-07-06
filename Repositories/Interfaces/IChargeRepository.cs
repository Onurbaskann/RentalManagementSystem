using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IChargeRepository : IBaseRepository<Charge>
{
    // Okuma — DTO döner
    Task<List<TahakkukListItemDto>> GetListAsync(int? leaseId, List<int>? yetkiliPropertyIds, List<int>? yetkiliBirimIds = null);
    Task<PagedResult<TahakkukListItemDto>> GetPagedListAsync(TableQuery q, int? leaseId, List<int>? yetkiliPropertyIds, List<int>? yetkiliBirimIds = null);
    Task<TahakkukDetayDto?> GetDetayAsync(int id);

    // Manuel Borç — DTO döner
    Task<List<ManuelBorcListItemDto>> GetManuelBorcListAsync(List<int>? yetkiliPropertyIds, string? durum = null, string? baglanti = null, int? leaseId = null, List<int>? yetkiliBirimIds = null);
    Task<int> GetManuelBorcIptalSayisiAsync(List<int>? yetkiliPropertyIds, List<int>? yetkiliBirimIds = null);

    // Business logic — entity döner (tracked)
    Task<List<Charge>> GetGeciktirileceklerAsync(DateTime bugun);
    Task<Charge?> GetManuelBorcByIdAsync(int id);
    Task<List<Charge>> GetBekleyenBorclarAsync(DateTime limitVade, CancellationToken ct);

    // Hesaplama
    Task<decimal> GetOdenenTutarAsync(int tahakkukId);

    // Üretim yardımcıları (ChargeGenerationService için)
    Task<List<ChargeType>> GetAktifUretimBorcTipleriAsync();
    Task<List<Charge>> GetSilineceklerAsync(int leaseId, DateTime ilkGun);
    Task DeleteRangeAsync(IEnumerable<Charge> entities);
}

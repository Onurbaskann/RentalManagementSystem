using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

/// <summary>
/// Tahakkuk iş mantığı. Veritabanı erişimi ITahakkukRepository üzerinden sağlanır.
/// </summary>
public class TahakkukService : ITahakkukService
{
    private readonly ITahakkukRepository _repo;
    private readonly UserTasinmazYetkiService _yetkiService;

    public TahakkukService(ITahakkukRepository repo, UserTasinmazYetkiService yetkiService)
    {
        _repo         = repo;
        _yetkiService = yetkiService;
    }

    // ── Listeme ──────────────────────────────────────────────────────────────

    public async Task<List<KiraTahakkuk>> GetAllAsync(int? sozlesmeId = null, string? userId = null)
    {
        var yetkiliIds = await ResolveYetkiAsync(userId);
        return await _repo.GetAllAsync(sozlesmeId, yetkiliIds);
    }

    public async Task<PagedResult<KiraTahakkuk>> GetPagedAsync(TableQuery q, int? sozlesmeId = null, string? userId = null)
    {
        var yetkiliIds = await ResolveYetkiAsync(userId);
        return await _repo.GetPagedAsync(q, sozlesmeId, yetkiliIds);
    }

    public async Task<KiraTahakkuk?> GetByIdAsync(int id) =>
        await _repo.GetByIdAsync(id);

    // ── Gecikme Güncelleme ────────────────────────────────────────────────────

    public async Task GecikmeleriGuncelleAsync()
    {
        var gecikmisBekleyenler = await _repo.GetGeciktirileceklerAsync(DateTime.Today);

        foreach (var t in gecikmisBekleyenler)
            t.Durum = TahakkukDurumu.Gecikti;

        if (gecikmisBekleyenler.Count > 0)
            await _repo.SaveChangesAsync();
    }

    // ── Ödenen Tutar Güncelleme ───────────────────────────────────────────────

    public async Task OdenenTutarGuncelleAsync(int tahakkukId)
    {
        var tahakkuk = await _repo.FindAsync(tahakkukId);
        if (tahakkuk == null) return;

        var odenenTutar = await _repo.GetOdenenTutarAsync(tahakkukId);
        tahakkuk.OdenenTutar = odenenTutar;

        // İş kuralı: Durumu ödeme tutarına göre belirle
        tahakkuk.Durum = odenenTutar >= tahakkuk.ToplamTutar
            ? TahakkukDurumu.TamOdendi
            : odenenTutar > 0
                ? TahakkukDurumu.KismenOdendi
                : DateTime.Today > tahakkuk.VadeTarihi
                    ? TahakkukDurumu.Gecikti
                    : TahakkukDurumu.Bekleniyor;

        await _repo.SaveChangesAsync();
    }

    // ── Private Yardımcılar ───────────────────────────────────────────────────

    private async Task<List<int>?> ResolveYetkiAsync(string? userId) =>
        userId == null ? null : await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);
}

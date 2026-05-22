using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TahakkukService : ITahakkukService
{
    private readonly ITahakkukRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IUserTasinmazYetkiService _yetkiService;

    public TahakkukService(ITahakkukRepository repo, IUnitOfWork uow, IUserTasinmazYetkiService yetkiService)
    {
        _repo = repo;
        _uow = uow;
        _yetkiService = yetkiService;
    }

    // ── Listeleme ────────────────────────────────────────────────────────
    public async Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId = null, string? userId = null)
    {
        var yetkiliIds = await ResolveYetkiAsync(userId);
        return await _repo.GetListAsync(sozlesmeId, yetkiliIds);
    }

    public async Task<PagedResult<TahakkukListItemDto>> GetPagedAsync(TableQuery q, int? sozlesmeId = null, string? userId = null)
    {
        var yetkiliIds = await ResolveYetkiAsync(userId);
        return await _repo.GetPagedListAsync(q, sozlesmeId, yetkiliIds);
    }

    public Task<TahakkukDetayDto?> GetDetayAsync(int id) => _repo.GetDetayAsync(id);

    // ── Business: Gecikme Güncelleme ─────────────────────────────────────
    public async Task GecikmeleriGuncelleAsync()
    {
        var gecikmisBekleyenler = await _repo.GetGeciktirileceklerAsync(DateTime.Today);
        if (gecikmisBekleyenler.Count == 0) return;

        foreach (var t in gecikmisBekleyenler)
        {
            t.Durum = TahakkukDurumu.Gecikti;
            await _repo.UpdateAsync(t);
        }

        await _uow.SaveChangesAsync();
    }

    // ── Business: Ödenen Tutar Güncelleme ────────────────────────────────
    public async Task OdenenTutarGuncelleAsync(int tahakkukId)
    {
        var tahakkuk = await _repo.GetByIdAsync(tahakkukId);
        if (tahakkuk == null) return;

        var odenenTutar = await _repo.GetOdenenTutarAsync(tahakkukId);
        tahakkuk.OdenenTutar = odenenTutar;

        tahakkuk.Durum = odenenTutar >= tahakkuk.ToplamTutar
            ? TahakkukDurumu.TamOdendi
            : odenenTutar > 0
                ? TahakkukDurumu.KismenOdendi
                : DateTime.Today > tahakkuk.VadeTarihi
                    ? TahakkukDurumu.Gecikti
                    : TahakkukDurumu.Bekleniyor;

        await _repo.UpdateAsync(tahakkuk);
        await _uow.SaveChangesAsync();
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────
    private async Task<List<int>?> ResolveYetkiAsync(string? userId) =>
        userId == null ? null : await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);
}

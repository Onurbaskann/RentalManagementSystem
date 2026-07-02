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
    public TahakkukService(ITahakkukRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    // ── Listeleme ────────────────────────────────────────────────────────
    public async Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId = null, IReadOnlyList<int>? tasinmazIds = null, IReadOnlyList<int>? birimIds = null)
    {
        return await _repo.GetListAsync(sozlesmeId, tasinmazIds?.ToList(), birimIds?.ToList());
    }

    public async Task<PagedResult<TahakkukListItemDto>> GetPagedAsync(TableQuery q, int? sozlesmeId = null, IReadOnlyList<int>? tasinmazIds = null, IReadOnlyList<int>? birimIds = null)
    {
        return await _repo.GetPagedListAsync(q, sozlesmeId, tasinmazIds?.ToList(), birimIds?.ToList());
    }

    public Task<TahakkukDetayDto?> GetDetayAsync(int id) => _repo.GetDetayAsync(id);

    // ── Business: Gecikme Güncelleme ─────────────────────────────────────
    public async Task GecikmeleriGuncelleAsync()
    {
        var gecikmisBekleyenler = await _repo.GetGeciktirileceklerAsync(DateTime.Today);
        if (gecikmisBekleyenler.Count == 0) return;

        foreach (var t in gecikmisBekleyenler)
        {
            t.Durum = ChargeStatus.Overdue;
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
            ? ChargeStatus.Paid
            : odenenTutar > 0
                ? ChargeStatus.PartiallyPaid
                : DateTime.Today > tahakkuk.VadeTarihi
                    ? ChargeStatus.Overdue
                    : ChargeStatus.Pending;

        await _repo.UpdateAsync(tahakkuk);
        await _uow.SaveChangesAsync();
    }

}

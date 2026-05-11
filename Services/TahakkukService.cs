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

    // ── Tahakkuk Oluşturma ────────────────────────────────────────────────────

    public async Task<(bool Basarili, string? Hata)> OlusturAsync(int sozlesmeId, DateTime donemBaslangic)
    {
        var donemIlkGunu = new DateTime(donemBaslangic.Year, donemBaslangic.Month, 1);

        // Sözleşme bilgisi — repository üzerinden çekiyoruz
        var sozlesme = await _repo.GetSozlesmeAsync(sozlesmeId);
        if (sozlesme == null)
            return (false, "Sözleşme bulunamadı.");

        if (sozlesme.Durum == SozlesmeDurumu.Feshedildi)
            return (false, "Feshedilmiş sözleşme için tahakkuk oluşturulamaz.");

        var mevcutVar = await _repo.ExistsForDonemAsync(sozlesmeId, donemIlkGunu);
        if (mevcutVar)
            return (false, $"{donemIlkGunu:MMMM yyyy} dönemi için tahakkuk zaten mevcut.");

        // İş kuralı: KDV hesaplama
        var kdvTutari = sozlesme.KdvUygulanacakMi
            ? Math.Round(sozlesme.KiraBedeli * sozlesme.KdvOrani / 100, 2)
            : 0m;

        var tahakkuk = new KiraTahakkuk
        {
            KiraSozlesmesiId = sozlesmeId,
            DonemBaslangic   = donemIlkGunu,
            DonemBitis       = new DateTime(donemIlkGunu.Year, donemIlkGunu.Month,
                                    DateTime.DaysInMonth(donemIlkGunu.Year, donemIlkGunu.Month)),
            VadeTarihi       = donemIlkGunu,
            BeklenenTutar    = sozlesme.KiraBedeli,
            KdvTutari        = kdvTutari,
            ToplamTutar      = sozlesme.KiraBedeli + kdvTutari,
            OdenenTutar      = 0,
            Durum            = TahakkukDurumu.Bekleniyor,
            KaynakTipi       = TahakkukKaynakTipi.Otomatik,
            OlusturmaTarihi  = DateTime.Now
        };

        await _repo.AddAsync(tahakkuk);
        await _repo.SaveChangesAsync();
        return (true, null);
    }

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

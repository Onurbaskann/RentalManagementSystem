using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TahakkukUretimService : ITahakkukUretimService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IRateResolverService _rateResolver;

    public TahakkukUretimService(ApplicationDbContext ctx, IRateResolverService rateResolver)
    {
        _ctx = ctx;
        _rateResolver = rateResolver;
    }

    public async Task UretSozlesmeIcinAsync(int sozlesmeId)
    {
        var sozlesme = await _ctx.Sozlesmeler
            .Include(s => s.Birim)
            .FirstOrDefaultAsync(s => s.Id == sozlesmeId);
        if (sozlesme == null) return;

        var aktifBorcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif && !b.TekSeferlikMi)
            .OrderBy(b => b.Sira)
            .ToListAsync();

        var depozitoBt = (sozlesme.Depozito.HasValue && sozlesme.Depozito.Value > 0)
            ? await _ctx.BorcTipleri.FirstOrDefaultAsync(b => b.Kod == "DEPOZITO" && b.Aktif)
            : null;

        foreach (var donemIlkGunu in GetDonemler(sozlesme.BaslangicTarihi, sozlesme.BitisTarihi))
        {
            var mevcutVar = await _ctx.KiraTahakkuklar
                .AnyAsync(t => t.KiraSozlesmesiId == sozlesmeId && t.DonemBaslangic == donemIlkGunu);
            if (mevcutVar) continue;

            var proRata = HesaplaProRataKatsayi(donemIlkGunu, sozlesme.BaslangicTarihi, sozlesme.BitisTarihi);
            var kalemler = new List<TahakkukKalemi>();

            foreach (var bt in aktifBorcTipleri)
            {
                var snapshot = await _rateResolver.ResolveAsync(sozlesmeId, sozlesme.BirimId, bt.Id, donemIlkGunu);
                if (snapshot == null) continue;

                var carpanBase = snapshot.HesaplamaYontemi == HesaplamaYontemi.M2
                    ? sozlesme.Birim.Yuzolcumu
                    : 1m;

                var tutar = Math.Round(snapshot.BirimDeger * carpanBase * proRata, 2);
                var kdvTutari = Math.Round(tutar * snapshot.KdvOrani / 100, 2);

                kalemler.Add(new TahakkukKalemi
                {
                    BorcTipiId = bt.Id,
                    Aciklama = bt.Ad,
                    HesaplamaYontemi = snapshot.HesaplamaYontemi,
                    BirimDeger = snapshot.BirimDeger,
                    Carpan = Math.Round(carpanBase * proRata, 6),
                    Tutar = tutar,
                    KdvOrani = snapshot.KdvOrani,
                    KdvTutari = kdvTutari,
                    ToplamTutar = tutar + kdvTutari,
                    KaynakTipi = snapshot.KaynakTipi
                });
            }

            bool isFirstPeriod = donemIlkGunu.Year == sozlesme.BaslangicTarihi.Year
                && donemIlkGunu.Month == sozlesme.BaslangicTarihi.Month;
            if (isFirstPeriod && depozitoBt != null)
            {
                kalemler.Add(new TahakkukKalemi
                {
                    BorcTipiId       = depozitoBt.Id,
                    Aciklama         = depozitoBt.Ad,
                    HesaplamaYontemi = HesaplamaYontemi.Sabit,
                    BirimDeger       = sozlesme.Depozito!.Value,
                    Carpan           = 1m,
                    Tutar            = sozlesme.Depozito.Value,
                    KdvOrani         = 0m,
                    KdvTutari        = 0m,
                    ToplamTutar      = sozlesme.Depozito.Value,
                    KaynakTipi       = KaynakTipi.Sozlesme
                });
            }

            var ayBitis = donemIlkGunu.AddMonths(1).AddDays(-1);
            var donemBitis = sozlesme.BitisTarihi < ayBitis ? sozlesme.BitisTarihi : ayBitis;

            var tahakkuk = new KiraTahakkuk
            {
                KiraSozlesmesiId = sozlesmeId,
                DonemBaslangic = donemIlkGunu,
                DonemBitis = donemBitis,
                VadeTarihi = donemIlkGunu,
                BeklenenTutar = kalemler.Sum(k => k.Tutar),
                KdvTutari = kalemler.Sum(k => k.KdvTutari),
                ToplamTutar = kalemler.Sum(k => k.ToplamTutar),
                OdenenTutar = 0,
                Durum = TahakkukDurumu.Bekleniyor,
                OlusturmaTarihi = DateTime.Now,
                Kalemler = kalemler
            };

            _ctx.KiraTahakkuklar.Add(tahakkuk);
        }

        await _ctx.SaveChangesAsync();
    }

    public async Task YenidenUretAsync(int sozlesmeId, DateTime baslangicTarihi)
    {
        var ilkGun = new DateTime(baslangicTarihi.Year, baslangicTarihi.Month, 1);

        var silinecekler = await _ctx.KiraTahakkuklar
            .Where(t => t.KiraSozlesmesiId == sozlesmeId
                && t.DonemBaslangic >= ilkGun
                && t.Durum != TahakkukDurumu.TamOdendi)
            .ToListAsync();

        _ctx.KiraTahakkuklar.RemoveRange(silinecekler);
        await _ctx.SaveChangesAsync();

        await UretSozlesmeIcinAsync(sozlesmeId);
    }

    public async Task IptalEtFutureTahakkuklarAsync(int sozlesmeId, DateTime fesihTarihi)
    {
        var ilkGun = new DateTime(fesihTarihi.Year, fesihTarihi.Month, 1).AddMonths(1);

        var iptalEdilecekler = await _ctx.KiraTahakkuklar
            .Where(t => t.KiraSozlesmesiId == sozlesmeId
                && t.DonemBaslangic >= ilkGun
                && t.Durum != TahakkukDurumu.TamOdendi)
            .ToListAsync();

        foreach (var t in iptalEdilecekler)
            t.Durum = TahakkukDurumu.IptalEdildi;

        if (iptalEdilecekler.Count > 0)
            await _ctx.SaveChangesAsync();
    }

    private static decimal HesaplaProRataKatsayi(DateTime donemIlkGunu, DateTime sozlesmeBaslangic, DateTime sozlesmeBitis)
    {
        var ayGunSayisi = DateTime.DaysInMonth(donemIlkGunu.Year, donemIlkGunu.Month);
        var ayBitis = donemIlkGunu.AddMonths(1).AddDays(-1);

        var etkinBaslangic = sozlesmeBaslangic > donemIlkGunu ? sozlesmeBaslangic : donemIlkGunu;
        var etkinBitis = sozlesmeBitis < ayBitis ? sozlesmeBitis : ayBitis;

        var gunSayisi = (etkinBitis - etkinBaslangic).Days + 1;
        return (decimal)gunSayisi / ayGunSayisi;
    }

    private static IEnumerable<DateTime> GetDonemler(DateTime baslangic, DateTime bitis)
    {
        var ay = new DateTime(baslangic.Year, baslangic.Month, 1);
        var sonAy = new DateTime(bitis.Year, bitis.Month, 1);
        while (ay <= sonAy)
        {
            yield return ay;
            ay = ay.AddMonths(1);
        }
    }
}

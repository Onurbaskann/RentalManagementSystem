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
            .Where(b => b.Aktif && (b.Davranis == BorcTipiDavranisi.AylikSabit || b.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik))
            .OrderBy(b => b.Sira)
            .ToListAsync();



        foreach (var donemIlkGunu in GetDonemler(sozlesme.BaslangicTarihi, sozlesme.BitisTarihi))
        {
            var mevcutVar = await _ctx.KiraTahakkuklar
                .AnyAsync(t => t.KiraSozlesmesiId == sozlesmeId
                    && t.DonemBaslangic == donemIlkGunu
                    && t.KaynakTipi == TahakkukKaynakTipi.Otomatik);
            if (mevcutVar) continue;

            var proRata = HesaplaProRataKatsayi(donemIlkGunu, sozlesme.BaslangicTarihi, sozlesme.BitisTarihi);
            
            var composedPreviews = await ComposeKalemlerAsync(sozlesme.BirimId, sozlesme.KiraciId, donemIlkGunu, sozlesmeId);
            var kalemler = new List<TahakkukKalemi>();

            foreach (var preview in composedPreviews)
            {
                var kalemProRata = preview.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik ? 1m : proRata;
                var tutar = Math.Round(preview.Tutar * kalemProRata, 2);
                var kdvTutari = Math.Round(tutar * preview.KdvOrani / 100, 2);

                kalemler.Add(new TahakkukKalemi
                {
                    BorcTipiId = preview.BorcTipiId,
                    Aciklama = preview.Aciklama ?? preview.BorcTipiAd,
                    HesaplamaYontemi = preview.HesaplamaYontemi,
                    BirimDeger = preview.BirimDeger,
                    Carpan = Math.Round(preview.Carpan * kalemProRata, 6),
                    Tutar = tutar,
                    KdvOrani = preview.KdvOrani,
                    KdvTutari = kdvTutari,
                    ToplamTutar = tutar + kdvTutari,
                    KaynakTipi = preview.KaynakTipi
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
                KaynakTipi = TahakkukKaynakTipi.Otomatik,
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
                && t.Durum != TahakkukDurumu.TamOdendi
                && t.KaynakTipi == TahakkukKaynakTipi.Otomatik)
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
                && t.Durum != TahakkukDurumu.TamOdendi
                && t.KaynakTipi == TahakkukKaynakTipi.Otomatik)
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

    public async Task<IList<Models.DTOs.TahakkukKalemiPreview>> ComposeKalemlerAsync(int birimId, int kiraciId, DateTime donem, int? sozlesmeId = null)
    {
        var birim = await _ctx.Birimler.FindAsync(birimId);
        if (birim == null) return new List<Models.DTOs.TahakkukKalemiPreview>();

        var aktifBorcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif && (b.Davranis == BorcTipiDavranisi.AylikSabit || b.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik))
            .OrderBy(b => b.Sira)
            .ToListAsync();

        var previewList = new List<Models.DTOs.TahakkukKalemiPreview>();

        foreach (var bt in aktifBorcTipleri)
        {
            // Tek seferlik kalemleri sadece ilk ayda göster/hesapla
            if (bt.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik)
            {
                DateTime? start = null;
                if (sozlesmeId.HasValue)
                {
                    start = await _ctx.Sozlesmeler
                        .Where(s => s.Id == sozlesmeId.Value)
                        .Select(s => s.BaslangicTarihi)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    // Yeni sözleşme oluştururken 'donem' başlangıç tarihi olarak kabul edilir
                    start = donem;
                }

                if (start.HasValue && (donem.Year != start.Value.Year || donem.Month != start.Value.Month))
                {
                    continue; // İlk ay değilse tek seferlik kalemi ekleme
                }
            }

            RateSnapshot? snapshot = await _rateResolver.ResolveAsync(sozlesmeId, kiraciId, birimId, bt.Id, donem);

            if (snapshot != null)
            {
                var carpanBase = snapshot.HesaplamaYontemi == HesaplamaYontemi.M2 ? birim.Yuzolcumu : 1m;
                var tutar = Math.Round(snapshot.BirimDeger * carpanBase, 2);
                var kdvTutari = Math.Round(tutar * snapshot.KdvOrani / 100, 2);

                previewList.Add(new Models.DTOs.TahakkukKalemiPreview
                {
                    BorcTipiId = bt.Id,
                    BorcTipiAd = bt.Ad,
                    BorcTipiKod = bt.Kod,
                    Davranis = bt.Davranis,
                    HesaplamaYontemi = snapshot.HesaplamaYontemi,
                    BirimDeger = snapshot.BirimDeger,
                    Carpan = carpanBase,
                    Tutar = tutar,
                    KdvOrani = snapshot.KdvOrani,
                    KdvTutari = kdvTutari,
                    ToplamTutar = tutar + kdvTutari,
                    KaynakTipi = snapshot.KaynakTipi,
                    RateBulundu = true,
                    Aciklama = bt.Ad
                });
            }
            else
            {
                previewList.Add(new Models.DTOs.TahakkukKalemiPreview
                {
                    BorcTipiId = bt.Id,
                    BorcTipiAd = bt.Ad,
                    BorcTipiKod = bt.Kod,
                    Davranis = bt.Davranis,
                    HesaplamaYontemi = HesaplamaYontemi.Sabit,
                    BirimDeger = 0m,
                    Carpan = 0m,
                    Tutar = 0m,
                    KdvOrani = 0m,
                    KdvTutari = 0m,
                    ToplamTutar = 0m,
                    KaynakTipi = KaynakTipi.Bulunamadi,
                    RateBulundu = false,
                    Aciklama = $"{bt.Ad} (Fiyat Tanımsız)"
                });
            }
        }

        return previewList;
    }
}

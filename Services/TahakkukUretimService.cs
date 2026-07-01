using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TahakkukUretimService : ITahakkukUretimService, ITransactionalService
{
    private readonly ITahakkukRepository _tahakkukRepo;
    private readonly IUnitOfWork _uow;
    private readonly IRateResolverService _rateResolver;
    private readonly ISozlesmeRepository _sozlesmeRepo;
    private readonly IBirimRepository _birimRepo;

    public TahakkukUretimService(
        ITahakkukRepository tahakkukRepo,
        IUnitOfWork uow,
        IRateResolverService rateResolver,
        ISozlesmeRepository sozlesmeRepo,
        IBirimRepository birimRepo)
    {
        _tahakkukRepo = tahakkukRepo;
        _uow = uow;
        _rateResolver = rateResolver;
        _sozlesmeRepo = sozlesmeRepo;
        _birimRepo = birimRepo;
    }

    public async Task UretSozlesmeIcinAsync(int sozlesmeId)
    {
        var sozlesme = await _sozlesmeRepo.GetByIdAsync(sozlesmeId);
        if (sozlesme == null) return;

        foreach (var donemIlkGunu in GetDonemler(sozlesme.BaslangicTarihi, sozlesme.BitisTarihi))
        {
            var mevcutVar = await _tahakkukRepo.AnyAsync(t => t.KiraSozlesmesiId == sozlesmeId
                && t.DonemBaslangic == donemIlkGunu
                && t.KaynakTipi == TahakkukKaynakTipi.Sozlesme);
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

            var tahakkuk = new Tahakkuk
            {
                KiraciId = sozlesme.KiraciId,
                BirimId = sozlesme.BirimId,
                KiraSozlesmesiId = sozlesmeId,
                DonemBaslangic = donemIlkGunu,
                DonemBitis = donemBitis,
                VadeTarihi = HesaplaVadeTarihi(donemIlkGunu, sozlesme.VadeKuraliTipi, sozlesme.VadeGunu),
                BeklenenTutar = kalemler.Sum(k => k.Tutar),
                KdvTutari = kalemler.Sum(k => k.KdvTutari),
                ToplamTutar = kalemler.Sum(k => k.ToplamTutar),
                OdenenTutar = 0,
                Durum = TahakkukDurumu.Bekleniyor,
                KaynakTipi = TahakkukKaynakTipi.Sozlesme,
                Kalemler = kalemler
            };

            await _tahakkukRepo.AddAsync(tahakkuk);
        }

        await _uow.SaveChangesAsync();
    }

    public async Task YenidenUretAsync(int sozlesmeId, DateTime baslangicTarihi)
    {
        var ilkGun = new DateTime(baslangicTarihi.Year, baslangicTarihi.Month, 1);
        var silinecekler = await _tahakkukRepo.GetSilineceklerAsync(sozlesmeId, ilkGun);
        await _tahakkukRepo.DeleteRangeAsync(silinecekler);
        await _uow.SaveChangesAsync();
        await UretSozlesmeIcinAsync(sozlesmeId);
    }

    public async Task BekleyenVadeleriYenidenHesaplaAsync(int sozlesmeId)
    {
        var sozlesme = await _sozlesmeRepo.GetByIdAsync(sozlesmeId);
        if (sozlesme == null) return;

        var hedefDurumlar = new[] { TahakkukDurumu.Bekleniyor, TahakkukDurumu.KismenOdendi, TahakkukDurumu.Gecikti };
        var bekleyenler = await _tahakkukRepo.GetAllAsync(t =>
            t.KiraSozlesmesiId == sozlesmeId
            && t.KaynakTipi == TahakkukKaynakTipi.Sozlesme
            && hedefDurumlar.Contains(t.Durum));

        if (bekleyenler.Count == 0) return;

        var bugun = DateTime.Today;
        foreach (var t in bekleyenler)
        {
            t.VadeTarihi = HesaplaVadeTarihi(t.DonemBaslangic, sozlesme.VadeKuraliTipi, sozlesme.VadeGunu);

            t.Durum = t.OdenenTutar >= t.ToplamTutar
                ? TahakkukDurumu.TamOdendi
                : t.OdenenTutar > 0
                    ? TahakkukDurumu.KismenOdendi
                    : bugun > t.VadeTarihi
                        ? TahakkukDurumu.Gecikti
                        : TahakkukDurumu.Bekleniyor;
        }

        await _uow.SaveChangesAsync();
    }

    public async Task IptalEtFutureTahakkuklarAsync(int sozlesmeId, DateTime fesihTarihi)
    {
        var ilkGun = new DateTime(fesihTarihi.Year, fesihTarihi.Month, 1).AddMonths(1);
        var iptalEdilecekler = await _tahakkukRepo.GetAllAsync(t =>
            t.KiraSozlesmesiId == sozlesmeId
            && t.DonemBaslangic >= ilkGun
            && t.Durum != TahakkukDurumu.TamOdendi
            && t.KaynakTipi == TahakkukKaynakTipi.Sozlesme);

        foreach (var t in iptalEdilecekler)
            t.Durum = TahakkukDurumu.IptalEdildi;

        if (iptalEdilecekler.Count > 0)
            await _uow.SaveChangesAsync();
    }

    private static DateTime HesaplaVadeTarihi(DateTime donemIlkGunu, VadeKuraliTipi tip, int vadeGunu)
    {
        return tip switch
        {
            VadeKuraliTipi.SabitAyGunu =>
                new DateTime(donemIlkGunu.Year, donemIlkGunu.Month,
                    Math.Min(Math.Max(vadeGunu, 1), DateTime.DaysInMonth(donemIlkGunu.Year, donemIlkGunu.Month))),
            VadeKuraliTipi.DonemBasiOfset =>
                donemIlkGunu.AddDays(Math.Max(vadeGunu - 1, 0)),
            _ => donemIlkGunu
        };
    }

    private static decimal HesaplaProRataKatsayi(DateTime donemIlkGunu, DateTime sozlesmeBaslangic, DateTime sozlesmeBitis)
    {
        var ayBitis = donemIlkGunu.AddMonths(1).AddDays(-1);
        var etkinBaslangic = sozlesmeBaslangic > donemIlkGunu ? sozlesmeBaslangic : donemIlkGunu;
        var etkinBitis = sozlesmeBitis < ayBitis ? sozlesmeBitis : ayBitis;

        if (etkinBaslangic == donemIlkGunu && etkinBitis == ayBitis)
            return 1.0m;

        var gunSayisi = (etkinBitis - etkinBaslangic).Days + 1;
        return Math.Min(1.0m, (decimal)gunSayisi / 30m);
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
        var birim = await _birimRepo.GetByIdAsync(birimId);
        if (birim == null) return new List<Models.DTOs.TahakkukKalemiPreview>();

        var aktifBorcTipleri = await _tahakkukRepo.GetAktifUretimBorcTipleriAsync();
        var previewList = new List<Models.DTOs.TahakkukKalemiPreview>();

        foreach (var bt in aktifBorcTipleri)
        {
            if (bt.Davranis == BorcTipiDavranisi.IlkAyTekSeferlik)
            {
                DateTime? start = null;
                if (sozlesmeId.HasValue)
                {
                    start = await _sozlesmeRepo.GetByIdAsync<DateTime?>(sozlesmeId.Value, s => s.BaslangicTarihi);
                }
                else
                {
                    start = donem;
                }

                if (start.HasValue && (donem.Year != start.Value.Year || donem.Month != start.Value.Month))
                    continue;
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
                    KaynakTipi = KalemKaynakTipi.TanimsizTarife,
                    RateBulundu = false,
                    Aciklama = $"{bt.Ad} (Fiyat Tanımsız)"
                });
            }
        }

        return previewList;
    }
}

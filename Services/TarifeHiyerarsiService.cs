using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TarifeHiyerarsiService : ITarifeHiyerarsiService
{
    private readonly ApplicationDbContext _ctx;

    public TarifeHiyerarsiService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<ParentTarifeKartViewModel?> GetParentForAsync(
        TarifeHiyerarsiKatmani katman,
        int? tasinmazId = null,
        int? birimId    = null,
        int? kategoriId = null,
        int? yil        = null)
    {
        int hedefYil = yil ?? DateTime.Now.Year;

        // Sozlesme katmanı: önce BirimRate'e bak
        if (katman == TarifeHiyerarsiKatmani.Sozlesme && birimId.HasValue)
        {
            IQueryable<BirimRate> bq = _ctx.BirimRateler
                .Include(r => r.Kategori)
                .Include(r => r.BorcTipi)
                .Where(r => r.BirimId == birimId.Value);
            if (kategoriId.HasValue)
                bq = bq.Where(r => r.KiraciKategoriId == kategoriId.Value);

            var rateler = await bq
                .OrderBy(r => r.Kategori.Sira)
                .ThenBy(r => r.BorcTipi.Sira)
                .ToListAsync();

            if (rateler.Count > 0)
                return new ParentTarifeKartViewModel
                {
                    KaynakAdi = "Birim Tarifesi",
                    Satirlar  = rateler.Select(r => new ParentTarifeSatir
                    {
                        KategoriAd       = r.Kategori.Ad,
                        BorcTipiAd       = r.BorcTipi.Ad,
                        HesaplamaYontemi = r.HesaplamaYontemi,
                        BirimDeger       = r.BirimDeger,
                        KdvOrani         = r.KdvOrani
                    }).ToList()
                };

            if (!tasinmazId.HasValue)
            {
                var birim = await _ctx.Birimler.FindAsync(birimId.Value);
                tasinmazId = birim?.TasinmazId;
            }
        }

        // Birim veya Sozlesme katmanı: TasinmazKiraciKategoriFiyat'a bak
        if (katman is TarifeHiyerarsiKatmani.Birim or TarifeHiyerarsiKatmani.Sozlesme
            && tasinmazId.HasValue)
        {
            IQueryable<TasinmazKiraciKategoriFiyat> tq = _ctx.TasinmazKiraciKategoriFiyatlari
                .Include(f => f.Kategori)
                .Include(f => f.BorcTipi)
                .Where(f => f.TasinmazId == tasinmazId.Value && f.Aktif);
            if (kategoriId.HasValue)
                tq = tq.Where(f => f.KiraciKategoriId == kategoriId.Value);

            var fiyatlar = await tq
                .OrderBy(f => f.Kategori.Sira)
                .ThenBy(f => f.BorcTipi.Sira)
                .ToListAsync();

            if (fiyatlar.Count > 0)
                return new ParentTarifeKartViewModel
                {
                    KaynakAdi = "Taşınmaz Tarifesi",
                    Satirlar  = fiyatlar.Select(f => new ParentTarifeSatir
                    {
                        KategoriAd       = f.Kategori.Ad,
                        BorcTipiAd       = f.BorcTipi.Ad,
                        HesaplamaYontemi = f.HesaplamaYontemi,
                        BirimDeger       = f.BirimDeger,
                        KdvOrani         = f.KdvOrani
                    }).ToList()
                };
        }

        // Her katman için sonuç: Genel Tarife
        IQueryable<TarifeKalemi> kq = _ctx.TarifeKalemleri
            .Include(k => k.Kategori)
            .Include(k => k.BorcTipi)
            .Where(k => k.Yil == hedefYil && k.Aktif
                     && k.BorcTipi.Davranis != BorcTipiDavranisi.KullaniciManuel
                     && k.BorcTipi.Davranis != BorcTipiDavranisi.RezervasyonOzel);
        if (kategoriId.HasValue)
            kq = kq.Where(k => k.KiraciKategoriId == kategoriId.Value);

        var kalemler = await kq
            .OrderBy(k => k.Kategori.Sira)
            .ThenBy(k => k.BorcTipi.Sira)
            .ToListAsync();

        // O yıl tarifesi yok — boş kart döner, partial "tanımlanmamış" mesajı gösterir
        if (kalemler.Count == 0)
            return new ParentTarifeKartViewModel
            {
                KaynakAdi = $"Genel Tarife - {hedefYil}",
                Satirlar  = []
            };

        return new ParentTarifeKartViewModel
        {
            KaynakAdi = $"Genel Tarife - {hedefYil}",
            Satirlar  = kalemler.Select(k => new ParentTarifeSatir
            {
                KategoriAd       = k.Kategori.Ad,
                BorcTipiAd       = k.BorcTipi.Ad,
                HesaplamaYontemi = k.HesaplamaYontemi,
                BirimDeger       = k.BirimDeger,
                KdvOrani         = k.KdvOrani
            }).ToList()
        };
    }

    public async Task<ParentRezervasyonTarifeKartViewModel?> GetRezervasyonParentForAsync(int? yil = null)
    {
        int hedefYil = yil ?? DateTime.Now.Year;

        var satirlar = await _ctx.RezervasyonUcretler
            .Include(r => r.BirimTuru)
            .Where(r => r.BirimId == null && r.BirimTuruId != null && r.Yil == hedefYil && r.Aktif && r.BirimTuru!.Aktif)
            .OrderBy(r => r.BirimTuru!.Sira)
            .Select(r => new ParentRezervasyonTarifeSatir
            {
                BirimTuruAd                 = r.BirimTuru!.Ad,
                UcretsizSureDakika          = r.UcretsizSureDakika,
                UcretlendirmePeriyoduDakika = r.UcretlendirmePeriyoduDakika,
                PeriyotUcreti               = r.PeriyotUcreti,
                KdvOrani                    = r.KdvOrani
            })
            .ToListAsync();

        if (satirlar.Count == 0)
            return new ParentRezervasyonTarifeKartViewModel
            {
                KaynakAdi = $"Rezervasyon Tarifesi - {hedefYil}",
                Satirlar  = []
            };

        return new ParentRezervasyonTarifeKartViewModel
        {
            KaynakAdi = $"Rezervasyon Tarifesi - {hedefYil}",
            Satirlar  = satirlar
        };
    }
}

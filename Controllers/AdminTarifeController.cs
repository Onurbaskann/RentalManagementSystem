using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Controllers;

[Route("Admin/Tarife")]
public class AdminTarifeController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public AdminTarifeController(ApplicationDbContext ctx) => _ctx = ctx;

    [Authorize(Policy = PermissionCatalog.Tarife.View)]
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tarifeler = await _ctx.Tarifeler
            .Include(t => t.Kalemler)
            .OrderByDescending(t => t.Yil)
            .ToListAsync();
        return View(tarifeler);
    }

    [Authorize(Policy = PermissionCatalog.Tarife.View)]
    [HttpGet("Yil/{yil:int}")]
    public async Task<IActionResult> Detay(int yil)
    {
        var tarife = await _ctx.Tarifeler
            .Include(t => t.Kalemler)
            .FirstOrDefaultAsync(t => t.Yil == yil);

        if (tarife == null) return NotFound();

        var kategoriler = await _ctx.KiraciKategorileri
            .Where(k => k.Aktif)
            .OrderBy(k => k.Sira)
            .ToListAsync();

        var borcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif && b.Davranis != BorcTipiDavranisi.KullaniciManuel && b.Davranis != BorcTipiDavranisi.RezervasyonOzel)
            .OrderBy(b => b.Sira)
            .ToListAsync();

        var vm = new TarifeMatrisViewModel
        {
            TarifeId = tarife.Id,
            Yil      = tarife.Yil,
            Aciklama = tarife.Aciklama,
            Aktif    = tarife.Aktif,
            Kolonlar = borcTipleri.Select(bt => new TarifeMatrisBorcTipiKolon
            {
                BorcTipiId  = bt.Id,
                BorcTipiAd  = bt.Ad,
                BorcTipiKod = bt.Kod
            }).ToList(),
            Satirlar = kategoriler.Select(kat => new TarifeMatrisSatir
            {
                KiraciKategoriId  = kat.Id,
                KiraciKategoriAd  = kat.Ad,
                Hucreler = borcTipleri.Select(bt =>
                {
                    var mevcut = tarife.Kalemler.FirstOrDefault(k =>
                        k.KiraciKategoriId == kat.Id && k.BorcTipiId == bt.Id);
                    return new TarifeMatrisHucre
                    {
                        KalemId          = mevcut?.Id ?? 0,
                        KiraciKategoriId = kat.Id,
                        BorcTipiId       = bt.Id,
                        HesaplamaYontemi = mevcut?.HesaplamaYontemi ?? HesaplamaYontemi.Sabit,
                        BirimDeger       = mevcut?.BirimDeger ?? 0,
                        KdvOrani         = mevcut?.KdvOrani ?? 0
                    };
                }).ToList()
            }).ToList()
        };

        var rezervasyonBirimTurleri = await _ctx.BirimTurleri
            .Where(t => t.Aktif && t.RezervasyonYapilabilirMi)
            .OrderBy(t => t.Sira)
            .ToListAsync();

        var mevcutRezervasyonlar = await _ctx.RezervasyonGenelTarifeleri
            .Where(r => r.TarifeId == tarife.Id)
            .ToListAsync();

        vm.RezervasyonSatirlari = rezervasyonBirimTurleri.Select(bt =>
        {
            var mevcut = mevcutRezervasyonlar.FirstOrDefault(r => r.BirimTuruId == bt.Id);
            return new TarifeMatrisRezervasyonSatir
            {
                RezervasyonGenelTarifeId    = mevcut?.Id ?? 0,
                BirimTuruId                 = bt.Id,
                BirimTuruAd                 = bt.Ad,
                UcretsizSureDakika          = mevcut?.UcretsizSureDakika ?? 0,
                UcretlendirmePeriyoduDakika = mevcut?.UcretlendirmePeriyoduDakika ?? 60,
                PeriyotUcreti               = mevcut?.PeriyotUcreti ?? 0,
                KdvOrani                    = mevcut?.KdvOrani ?? 20
            };
        }).ToList();

        return View(vm);
    }

    [Authorize(Policy = PermissionCatalog.Tarife.Manage)]
    [HttpPost("Yil/{yil:int}/KalemGuncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KalemGuncelle(int yil, TarifeMatrisPostViewModel vm)
    {
        var tarife = await _ctx.Tarifeler
            .Include(t => t.Kalemler)
            .FirstOrDefaultAsync(t => t.Yil == yil);

        if (tarife == null) return NotFound();

        tarife.Aciklama = vm.Aciklama;

        foreach (var hucre in vm.Hucreler)
        {
            var mevcut = tarife.Kalemler.FirstOrDefault(k =>
                k.KiraciKategoriId == hucre.KiraciKategoriId && k.BorcTipiId == hucre.BorcTipiId);
            if (mevcut == null)
            {
                tarife.Kalemler.Add(new TarifeKalemi
                {
                    TarifeId         = tarife.Id,
                    KiraciKategoriId = hucre.KiraciKategoriId,
                    BorcTipiId       = hucre.BorcTipiId,
                    HesaplamaYontemi = hucre.HesaplamaYontemi,
                    BirimDeger       = hucre.BirimDeger,
                    KdvOrani         = hucre.KdvOrani
                });
            }
            else
            {
                mevcut.HesaplamaYontemi = hucre.HesaplamaYontemi;
                mevcut.BirimDeger       = hucre.BirimDeger;
                mevcut.KdvOrani         = hucre.KdvOrani;
            }
        }

        foreach (var rez in vm.RezervasyonHucreler)
        {
            var mevcut = await _ctx.RezervasyonGenelTarifeleri
                    .FirstOrDefaultAsync(r => r.TarifeId == tarife.Id && r.BirimTuruId == rez.BirimTuruId);

            if (mevcut == null)
            {
                _ctx.RezervasyonGenelTarifeleri.Add(new RezervasyonGenelTarife
                {
                    TarifeId                    = tarife.Id,
                    BirimTuruId                 = rez.BirimTuruId,
                    UcretsizSureDakika          = rez.UcretsizSureDakika,
                    UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika,
                    PeriyotUcreti               = rez.PeriyotUcreti,
                    KdvOrani                    = rez.KdvOrani,
                    OlusturmaTarihi             = DateTime.Now
                });
            }
            else
            {
                mevcut.UcretsizSureDakika          = rez.UcretsizSureDakika;
                mevcut.UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika;
                mevcut.PeriyotUcreti               = rez.PeriyotUcreti;
                mevcut.KdvOrani                    = rez.KdvOrani;
            }
        }

        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"{yil} yılı tarifeleri güncellendi.";
        return RedirectToAction(nameof(Detay), new { yil });
    }

    [Authorize(Policy = PermissionCatalog.Tarife.Manage)]
    [HttpGet("YilEkle")]
    public async Task<IActionResult> YilEkle()
    {
        var mevcutYillar = await _ctx.Tarifeler
            .OrderByDescending(t => t.Yil)
            .Select(t => new { t.Id, t.Yil })
            .ToListAsync();

        ViewBag.MevcutYillar = mevcutYillar;
        return View(new TarifeYilEkleViewModel { Yil = DateTime.Now.Year });
    }

    [Authorize(Policy = PermissionCatalog.Tarife.Manage)]
    [HttpPost("YilEkle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YilEkle(TarifeYilEkleViewModel vm)
    {
        if (await _ctx.Tarifeler.AnyAsync(t => t.Yil == vm.Yil))
        {
            ModelState.AddModelError(nameof(vm.Yil), "Bu yıl için zaten tarife mevcut.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.MevcutYillar = await _ctx.Tarifeler
                .OrderByDescending(t => t.Yil)
                .Select(t => new { t.Id, t.Yil })
                .ToListAsync();
            return View(vm);
        }

        var yeniTarife = new Tarife
        {
            Yil             = vm.Yil,
            Aciklama        = vm.Aciklama,
            Aktif           = true,
            OlusturmaTarihi = DateTime.Now
        };

        if (vm.KopyalaYilId.HasValue)
        {
            var kaynak = await _ctx.Tarifeler
                .Include(t => t.Kalemler)
                .FirstOrDefaultAsync(t => t.Id == vm.KopyalaYilId.Value);

            if (kaynak != null)
            {
                foreach (var kalem in kaynak.Kalemler)
                {
                    yeniTarife.Kalemler.Add(new TarifeKalemi
                    {
                        KiraciKategoriId = kalem.KiraciKategoriId,
                        BorcTipiId       = kalem.BorcTipiId,
                        HesaplamaYontemi = kalem.HesaplamaYontemi,
                        BirimDeger       = kalem.BirimDeger,
                        KdvOrani         = kalem.KdvOrani
                    });
                }

                var kaynakRezervasyonlar = await _ctx.RezervasyonGenelTarifeleri
                    .Where(r => r.TarifeId == kaynak.Id)
                    .ToListAsync();

                foreach (var rez in kaynakRezervasyonlar)
                {
                    _ctx.RezervasyonGenelTarifeleri.Add(new RezervasyonGenelTarife
                    {
                        Tarife = yeniTarife, // Bind to the new tariff object
                        BirimTuruId = rez.BirimTuruId,
                        UcretsizSureDakika = rez.UcretsizSureDakika,
                        UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika,
                        PeriyotUcreti = rez.PeriyotUcreti,
                        KdvOrani = rez.KdvOrani,
                        OlusturmaTarihi = DateTime.Now
                    });
                }
            }
        }
        else
        {
            var kategoriler = await _ctx.KiraciKategorileri.Where(k => k.Aktif).OrderBy(k => k.Sira).ToListAsync();
            var aktifBorcTipleri = await _ctx.BorcTipleri
                .Where(b => b.Aktif && b.Davranis != BorcTipiDavranisi.KullaniciManuel && b.Davranis != BorcTipiDavranisi.RezervasyonOzel)
                .OrderBy(b => b.Sira).ToListAsync();
            foreach (var kat in kategoriler)
            {
                foreach (var bt in aktifBorcTipleri)
                {
                    yeniTarife.Kalemler.Add(new TarifeKalemi
                    {
                        KiraciKategoriId = kat.Id,
                        BorcTipiId       = bt.Id,
                        HesaplamaYontemi = HesaplamaYontemi.Sabit,
                        BirimDeger       = 0,
                        KdvOrani         = 0
                    });
                }
            }

            var rezBirimTurleri = await _ctx.BirimTurleri
                .Where(t => t.Aktif && t.RezervasyonYapilabilirMi)
                .ToListAsync();
            foreach (var bt in rezBirimTurleri)
            {
                _ctx.RezervasyonGenelTarifeleri.Add(new RezervasyonGenelTarife
                {
                    Tarife = yeniTarife,
                    BirimTuruId = bt.Id,
                    UcretsizSureDakika = 0,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti = 0,
                    KdvOrani = 0,
                    OlusturmaTarihi = DateTime.Now
                });
            }
        }

        _ctx.Tarifeler.Add(yeniTarife);
        await _ctx.SaveChangesAsync();

        TempData["Success"] = $"{vm.Yil} yılı tarifeleri oluşturuldu.";
        return RedirectToAction(nameof(Detay), new { yil = vm.Yil });
    }

    [Authorize(Policy = PermissionCatalog.Tarife.Manage)]
    [HttpPost("DurumDegistir/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id)
    {
        var entity = await _ctx.Tarifeler.FindAsync(id);
        if (entity == null) return NotFound();
        entity.Aktif = !entity.Aktif;
        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"{entity.Yil} yılı tarifeleri {(entity.Aktif ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }
}

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
        var ozet = await _ctx.GenelTarifeler
            .GroupBy(k => k.Yil)
            .Select(g => new TarifeYilOzetiViewModel
            {
                Yil         = g.Key,
                Aktif       = g.Any(k => k.Aktif),
                KalemSayisi = g.Count()
            })
            .OrderByDescending(o => o.Yil)
            .ToListAsync();

        return View(ozet);
    }

    [Authorize(Policy = PermissionCatalog.Tarife.View)]
    [HttpGet("Yil/{yil:int}")]
    public async Task<IActionResult> Detay(int yil)
    {
        var kalemler = await _ctx.GenelTarifeler
            .Where(k => k.Yil == yil)
            .ToListAsync();

        if (kalemler.Count == 0) return NotFound();

        var kategoriler = await _ctx.Kategoriler
            .Where(k => k.Tipi == KategoriTipi.Kiraci && k.Aktif)
            .OrderBy(k => k.Sira)
            .ToListAsync();

        var borcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif && b.Davranis != BorcTipiDavranisi.KullaniciManuel && b.Davranis != BorcTipiDavranisi.RezervasyonOzel)
            .OrderBy(b => b.Sira)
            .ToListAsync();

        var vm = new TarifeMatrisViewModel
        {
            Yil   = yil,
            Aktif = kalemler.Any(k => k.Aktif),
            Kolonlar = borcTipleri.Select(bt => new TarifeMatrisBorcTipiKolon
            {
                BorcTipiId  = bt.Id,
                BorcTipiAd  = bt.Ad,
                BorcTipiKod = bt.Kod
            }).ToList(),
            Satirlar = kategoriler.Select(kat => new TarifeMatrisSatir
            {
                KiraciKategoriId = kat.Id,
                KiraciKategoriAd = kat.Ad,
                Hucreler = borcTipleri.Select(bt =>
                {
                    var mevcut = kalemler.FirstOrDefault(k =>
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

        var mevcutRezervasyonlar = await _ctx.RezervasyonTarifeler
            .Where(r => r.BirimId == null && r.Yil == yil)
            .ToListAsync();

        vm.RezervasyonSatirlari = rezervasyonBirimTurleri.Select(bt =>
        {
            var mevcut = mevcutRezervasyonlar.FirstOrDefault(r => r.BirimTuruId == bt.Id);
            return new TarifeMatrisRezervasyonSatir
            {
                RezervasyonTarifeId          = mevcut?.Id ?? 0,
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
        var mevcutKalemler = await _ctx.GenelTarifeler
            .Where(k => k.Yil == yil)
            .ToListAsync();

        foreach (var hucre in vm.Hucreler)
        {
            var mevcut = mevcutKalemler.FirstOrDefault(k =>
                k.KiraciKategoriId == hucre.KiraciKategoriId && k.BorcTipiId == hucre.BorcTipiId);
            if (mevcut == null)
            {
                _ctx.GenelTarifeler.Add(new GenelTarife
                {
                    Yil              = yil,
                    Aktif            = true,
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
            var mevcut = await _ctx.RezervasyonTarifeler
                .FirstOrDefaultAsync(r => r.BirimId == null && r.Yil == yil && r.BirimTuruId == rez.BirimTuruId);

            if (mevcut == null)
            {
                _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
                {
                    Yil                         = yil,
                    Aktif                       = true,
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
        var mevcutYillar = await _ctx.GenelTarifeler
            .Select(k => k.Yil)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        ViewBag.MevcutYillar = mevcutYillar;
        return View(new TarifeYilEkleViewModel { Yil = DateTime.Now.Year });
    }

    [Authorize(Policy = PermissionCatalog.Tarife.Manage)]
    [HttpPost("YilEkle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YilEkle(TarifeYilEkleViewModel vm)
    {
        if (await _ctx.GenelTarifeler.AnyAsync(k => k.Yil == vm.Yil))
        {
            ModelState.AddModelError(nameof(vm.Yil), "Bu yıl için zaten tarife mevcut.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.MevcutYillar = await _ctx.GenelTarifeler
                .Select(k => k.Yil)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            return View(vm);
        }

        if (vm.KopyalaYil.HasValue)
        {
            var kaynakKalemler = await _ctx.GenelTarifeler
                .Where(k => k.Yil == vm.KopyalaYil.Value)
                .ToListAsync();

            foreach (var kalem in kaynakKalemler)
            {
                _ctx.GenelTarifeler.Add(new GenelTarife
                {
                    Yil              = vm.Yil,
                    Aktif            = true,
                    KiraciKategoriId = kalem.KiraciKategoriId,
                    BorcTipiId       = kalem.BorcTipiId,
                    HesaplamaYontemi = kalem.HesaplamaYontemi,
                    BirimDeger       = kalem.BirimDeger,
                    KdvOrani         = kalem.KdvOrani
                });
            }

            var kaynakRezervasyonlar = await _ctx.RezervasyonTarifeler
                .Where(r => r.BirimId == null && r.Yil == vm.KopyalaYil.Value)
                .ToListAsync();

            foreach (var rez in kaynakRezervasyonlar)
            {
                _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
                {
                    Yil                         = vm.Yil,
                    Aktif                       = true,
                    BirimTuruId                 = rez.BirimTuruId,
                    UcretsizSureDakika          = rez.UcretsizSureDakika,
                    UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika,
                    PeriyotUcreti               = rez.PeriyotUcreti,
                    KdvOrani                    = rez.KdvOrani,
                    OlusturmaTarihi             = DateTime.Now
                });
            }
        }
        else
        {
            var kategoriler = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Kiraci && k.Aktif).OrderBy(k => k.Sira).ToListAsync();
            var aktifBorcTipleri = await _ctx.BorcTipleri
                .Where(b => b.Aktif && b.Davranis != BorcTipiDavranisi.KullaniciManuel && b.Davranis != BorcTipiDavranisi.RezervasyonOzel)
                .OrderBy(b => b.Sira).ToListAsync();

            foreach (var kat in kategoriler)
            {
                foreach (var bt in aktifBorcTipleri)
                {
                    _ctx.GenelTarifeler.Add(new GenelTarife
                    {
                        Yil              = vm.Yil,
                        Aktif            = true,
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
                _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
                {
                    Yil                         = vm.Yil,
                    Aktif                       = true,
                    BirimTuruId                 = bt.Id,
                    UcretsizSureDakika          = 0,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti               = 0,
                    KdvOrani                    = 0,
                    OlusturmaTarihi             = DateTime.Now
                });
            }
        }

        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"{vm.Yil} yılı tarifeleri oluşturuldu.";
        return RedirectToAction(nameof(Detay), new { yil = vm.Yil });
    }

    [Authorize(Policy = PermissionCatalog.Tarife.Manage)]
    [HttpPost("DurumDegistir/{yil:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int yil)
    {
        var kalem = await _ctx.GenelTarifeler.FirstOrDefaultAsync(k => k.Yil == yil);
        if (kalem == null) return NotFound();

        var yeniDeger = !kalem.Aktif;

        await _ctx.GenelTarifeler
            .Where(k => k.Yil == yil)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.Aktif, yeniDeger));

        await _ctx.RezervasyonTarifeler
            .Where(r => r.BirimId == null && r.Yil == yil)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Aktif, yeniDeger));

        TempData["Success"] = $"{yil} yılı tarifeleri {(yeniDeger ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }
}

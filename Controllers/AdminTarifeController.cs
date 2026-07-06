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

    [Authorize(Policy = PermissionCatalog.RateSchedule.Module)]
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var ozet = await _ctx.GenelTarifeler
            .GroupBy(k => k.Yil)
            .Select(g => new TarifeYilOzetiViewModel
            {
                Yil         = g.Key,
                Aktif       = g.Any(k => k.IsActive),
                KalemSayisi = g.Count()
            })
            .OrderByDescending(o => o.Yil)
            .ToListAsync();

        return View(ozet);
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Module)]
    [HttpGet("Yil/{yil:int}")]
    public async Task<IActionResult> Detay(int yil)
    {
        var kalemler = await _ctx.GenelTarifeler
            .Where(k => k.Yil == yil)
            .ToListAsync();

        if (kalemler.Count == 0) return NotFound();

        var kategoriler = await _ctx.Kategoriler
            .Where(k => k.Tipi == KategoriTipi.Tenant && k.IsActive)
            .OrderBy(k => k.Sira)
            .ToListAsync();

        var borcTipleri = await _ctx.ChargeTypes
            .Where(b => b.IsActive && b.Behavior != ChargeTypeBehavior.UserManual && b.Behavior != ChargeTypeBehavior.ReservationSpecific)
            .OrderBy(b => b.SortOrder)
            .ToListAsync();

        var vm = new TarifeMatrisViewModel
        {
            Yil   = yil,
            Aktif = kalemler.Any(k => k.IsActive),
            Kolonlar = borcTipleri.Select(bt => new TarifeMatrisBorcTipiKolon
            {
                ChargeTypeId  = bt.Id,
                ChargeTypeName  = bt.Name,
                ChargeTypeCode = bt.Code
            }).ToList(),
            Satirlar = kategoriler.Select(kat => new TarifeMatrisSatir
            {
                KiraciKategoriId = kat.Id,
                KiraciKategoriAd = kat.Ad,
                Hucreler = borcTipleri.Select(bt =>
                {
                    var mevcut = kalemler.FirstOrDefault(k =>
                        k.KiraciKategoriId == kat.Id && k.ChargeTypeId == bt.Id);
                    return new TarifeMatrisHucre
                    {
                        KalemId          = mevcut?.Id ?? 0,
                        KiraciKategoriId = kat.Id,
                        ChargeTypeId       = bt.Id,
                        CalculationMethod = mevcut?.CalculationMethod ?? CalculationMethod.Fixed,
                        UnitValue       = mevcut?.UnitValue ?? 0,
                        KdvRate         = mevcut?.KdvRate ?? 0
                    };
                }).ToList()
            }).ToList()
        };

        var rezervasyonBirimTurleri = await _ctx.BirimTurleri
            .Where(t => t.IsActive && t.RezervasyonYapilabilirMi)
            .OrderBy(t => t.Sira)
            .ToListAsync();

        var mevcutRezervasyonlar = await _ctx.RezervasyonTarifeler
            .Where(r => r.UnitId == null && r.Yil == yil)
            .ToListAsync();

        vm.RezervasyonSatirlari = rezervasyonBirimTurleri.Select(bt =>
        {
            var mevcut = mevcutRezervasyonlar.FirstOrDefault(r => r.UnitTypeId == bt.Id);
            return new TarifeMatrisRezervasyonSatir
            {
                RezervasyonTarifeId          = mevcut?.Id ?? 0,
                UnitTypeId                 = bt.Id,
                UnitTypeAd                 = bt.Ad,
                FreeDurationMinutes          = mevcut?.FreeDurationMinutes ?? 0,
                UcretlendirmePeriyoduDakika = mevcut?.UcretlendirmePeriyoduDakika ?? 60,
                PeriyotUcreti               = mevcut?.PeriyotUcreti ?? 0,
                KdvRate                    = mevcut?.KdvRate ?? 20
            };
        }).ToList();

        return View(vm);
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Edit)]
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
                k.KiraciKategoriId == hucre.KiraciKategoriId && k.ChargeTypeId == hucre.ChargeTypeId);
            if (mevcut == null)
            {
                _ctx.GenelTarifeler.Add(new GenelTarife
                {
                    Yil              = yil,
                    KiraciKategoriId = hucre.KiraciKategoriId,
                    ChargeTypeId       = hucre.ChargeTypeId,
                    CalculationMethod = hucre.CalculationMethod,
                    UnitValue       = hucre.UnitValue,
                    KdvRate         = hucre.KdvRate
                });
            }
            else
            {
                mevcut.CalculationMethod = hucre.CalculationMethod;
                mevcut.UnitValue       = hucre.UnitValue;
                mevcut.KdvRate         = hucre.KdvRate;
            }
        }

        foreach (var rez in vm.RezervasyonHucreler)
        {
            var mevcut = await _ctx.RezervasyonTarifeler
                .FirstOrDefaultAsync(r => r.UnitId == null && r.Yil == yil && r.UnitTypeId == rez.UnitTypeId);

            if (mevcut == null)
            {
                _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
                {
                    Yil                         = yil,
                    UnitTypeId                 = rez.UnitTypeId,
                    FreeDurationMinutes          = rez.FreeDurationMinutes,
                    UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika,
                    PeriyotUcreti               = rez.PeriyotUcreti,
                    KdvRate                    = rez.KdvRate
                });
            }
            else
            {
                mevcut.FreeDurationMinutes          = rez.FreeDurationMinutes;
                mevcut.UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika;
                mevcut.PeriyotUcreti               = rez.PeriyotUcreti;
                mevcut.KdvRate                    = rez.KdvRate;
            }
        }

        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"{yil} yılı tarifeleri güncellendi.";
        return RedirectToAction(nameof(Detay), new { yil });
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Edit)]
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

    [Authorize(Policy = PermissionCatalog.RateSchedule.Create)]
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
                    KiraciKategoriId = kalem.KiraciKategoriId,
                    ChargeTypeId       = kalem.ChargeTypeId,
                    CalculationMethod = kalem.CalculationMethod,
                    UnitValue       = kalem.UnitValue,
                    KdvRate         = kalem.KdvRate
                });
            }

            var kaynakRezervasyonlar = await _ctx.RezervasyonTarifeler
                .Where(r => r.UnitId == null && r.Yil == vm.KopyalaYil.Value)
                .ToListAsync();

            foreach (var rez in kaynakRezervasyonlar)
            {
                _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
                {
                    Yil                         = vm.Yil,
                    UnitTypeId                 = rez.UnitTypeId,
                    FreeDurationMinutes          = rez.FreeDurationMinutes,
                    UcretlendirmePeriyoduDakika = rez.UcretlendirmePeriyoduDakika,
                    PeriyotUcreti               = rez.PeriyotUcreti,
                    KdvRate                    = rez.KdvRate
                });
            }
        }
        else
        {
            var kategoriler = await _ctx.Kategoriler.Where(k => k.Tipi == KategoriTipi.Tenant && k.IsActive).OrderBy(k => k.Sira).ToListAsync();
            var aktifBorcTipleri = await _ctx.ChargeTypes
                .Where(b => b.IsActive && b.Behavior != ChargeTypeBehavior.UserManual && b.Behavior != ChargeTypeBehavior.ReservationSpecific)
                .OrderBy(b => b.SortOrder).ToListAsync();

            foreach (var kat in kategoriler)
            {
                foreach (var bt in aktifBorcTipleri)
                {
                    _ctx.GenelTarifeler.Add(new GenelTarife
                    {
                        Yil              = vm.Yil,
                        KiraciKategoriId = kat.Id,
                        ChargeTypeId       = bt.Id,
                        CalculationMethod = CalculationMethod.Fixed,
                        UnitValue       = 0,
                        KdvRate         = 0
                    });
                }
            }

            var rezBirimTurleri = await _ctx.BirimTurleri
                .Where(t => t.IsActive && t.RezervasyonYapilabilirMi)
                .ToListAsync();

            foreach (var bt in rezBirimTurleri)
            {
                _ctx.RezervasyonTarifeler.Add(new RezervasyonTarife
                {
                    Yil                         = vm.Yil,
                    UnitTypeId                 = bt.Id,
                    FreeDurationMinutes          = 0,
                    UcretlendirmePeriyoduDakika = 60,
                    PeriyotUcreti               = 0,
                    KdvRate                    = 0
                });
            }
        }

        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"{vm.Yil} yılı tarifeleri oluşturuldu.";
        return RedirectToAction(nameof(Detay), new { yil = vm.Yil });
    }

    [Authorize(Policy = PermissionCatalog.RateSchedule.Edit)]
    [HttpPost("DurumDegistir/{yil:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int yil)
    {
        var kalem = await _ctx.GenelTarifeler.FirstOrDefaultAsync(k => k.Yil == yil);
        if (kalem == null) return NotFound();

        var yeniDeger = !kalem.IsActive;

        await _ctx.GenelTarifeler
            .Where(k => k.Yil == yil)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.IsActive, yeniDeger));

        await _ctx.RezervasyonTarifeler
            .Where(r => r.UnitId == null && r.Yil == yil)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, yeniDeger));

        TempData["Success"] = $"{yil} yılı tarifeleri {(yeniDeger ? "aktif" : "pasif")} yapıldı.";
        return RedirectToAction(nameof(Index));
    }
}

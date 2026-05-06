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
            .Include(t => t.Kalemler).ThenInclude(k => k.BorcTipi)
            .FirstOrDefaultAsync(t => t.Yil == yil);

        if (tarife == null) return NotFound();

        var aktifBorcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif)
            .OrderBy(b => b.Sira)
            .ToListAsync();

        var kalemler = aktifBorcTipleri.Select(bt =>
        {
            var mevcut = tarife.Kalemler.FirstOrDefault(k => k.BorcTipiId == bt.Id);
            return new TarifeKalemSatiri
            {
                KalemId           = mevcut?.Id ?? 0,
                BorcTipiId        = bt.Id,
                BorcTipiAd        = bt.Ad,
                BorcTipiKod       = bt.Kod,
                HesaplamaYontemi  = mevcut?.HesaplamaYontemi ?? HesaplamaYontemi.Sabit,
                BirimDeger        = mevcut?.BirimDeger ?? 0,
                KdvOrani          = mevcut?.KdvOrani ?? 0
            };
        }).ToList();

        var vm = new TarifeDetayViewModel
        {
            TarifeId = tarife.Id,
            Yil      = tarife.Yil,
            Aciklama = tarife.Aciklama,
            Aktif    = tarife.Aktif,
            Kalemler = kalemler
        };

        return View(vm);
    }

    [Authorize(Policy = PermissionCatalog.Tarife.Manage)]
    [HttpPost("Yil/{yil:int}/KalemGuncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KalemGuncelle(int yil, TarifeDetayViewModel vm)
    {
        var tarife = await _ctx.Tarifeler
            .Include(t => t.Kalemler)
            .FirstOrDefaultAsync(t => t.Yil == yil);

        if (tarife == null) return NotFound();

        tarife.Aciklama = vm.Aciklama;

        foreach (var satir in vm.Kalemler)
        {
            var mevcut = tarife.Kalemler.FirstOrDefault(k => k.BorcTipiId == satir.BorcTipiId);
            if (mevcut == null)
            {
                tarife.Kalemler.Add(new TarifeKalemi
                {
                    TarifeId         = tarife.Id,
                    BorcTipiId       = satir.BorcTipiId,
                    HesaplamaYontemi = satir.HesaplamaYontemi,
                    BirimDeger       = satir.BirimDeger,
                    KdvOrani         = satir.KdvOrani
                });
            }
            else
            {
                mevcut.HesaplamaYontemi = satir.HesaplamaYontemi;
                mevcut.BirimDeger       = satir.BirimDeger;
                mevcut.KdvOrani         = satir.KdvOrani;
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
                        BorcTipiId       = kalem.BorcTipiId,
                        HesaplamaYontemi = kalem.HesaplamaYontemi,
                        BirimDeger       = kalem.BirimDeger,
                        KdvOrani         = kalem.KdvOrani
                    });
                }
            }
        }
        else
        {
            var aktifBorcTipleri = await _ctx.BorcTipleri.Where(b => b.Aktif).ToListAsync();
            foreach (var bt in aktifBorcTipleri)
            {
                yeniTarife.Kalemler.Add(new TarifeKalemi
                {
                    BorcTipiId       = bt.Id,
                    HesaplamaYontemi = HesaplamaYontemi.Sabit,
                    BirimDeger       = 0,
                    KdvOrani         = 0
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

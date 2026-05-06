using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Birim")]
public class BirimController : Controller
{
    private readonly ApplicationDbContext _ctx;

    public BirimController(ApplicationDbContext ctx) => _ctx = ctx;

    [Authorize(Policy = PermissionCatalog.Birim.ManageRate)]
    [HttpGet("{id:int}/OzelFiyat")]
    public async Task<IActionResult> OzelFiyat(int id)
    {
        var birim = await _ctx.Birimler
            .Include(b => b.Tasinmaz)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (birim == null) return NotFound();

        var aktifBorcTipleri = await _ctx.BorcTipleri
            .Where(b => b.Aktif)
            .OrderBy(b => b.Sira)
            .ToListAsync();

        var mevcutRateler = await _ctx.BirimRateler
            .Where(r => r.BirimId == id)
            .ToListAsync();

        var kalemler = aktifBorcTipleri.Select(bt =>
        {
            var rate = mevcutRateler.FirstOrDefault(r => r.BorcTipiId == bt.Id);
            return new BirimRateSatiri
            {
                RateId           = rate?.Id ?? 0,
                BorcTipiId       = bt.Id,
                BorcTipiAd       = bt.Ad,
                BorcTipiKod      = bt.Kod,
                OzelFiyatAktif   = rate != null,
                HesaplamaYontemi = rate?.HesaplamaYontemi ?? HesaplamaYontemi.Sabit,
                BirimDeger       = rate?.BirimDeger ?? 0,
                KdvOrani         = rate?.KdvOrani ?? 0
            };
        }).ToList();

        var vm = new BirimOzelFiyatViewModel
        {
            BirimId     = birim.Id,
            BirimAd     = birim.Ad,
            TasinmazId  = birim.TasinmazId,
            TasinmazAd  = birim.Tasinmaz.Ad,
            Kalemler    = kalemler
        };

        return View(vm);
    }

    [Authorize(Policy = PermissionCatalog.Birim.ManageRate)]
    [HttpPost("{id:int}/OzelFiyat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OzelFiyat(int id, BirimOzelFiyatViewModel vm)
    {
        var mevcutRateler = await _ctx.BirimRateler
            .Where(r => r.BirimId == id)
            .ToListAsync();

        foreach (var satir in vm.Kalemler)
        {
            var mevcut = mevcutRateler.FirstOrDefault(r => r.BorcTipiId == satir.BorcTipiId);

            if (satir.OzelFiyatAktif)
            {
                if (mevcut == null)
                {
                    _ctx.BirimRateler.Add(new BirimRate
                    {
                        BirimId          = id,
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
            else if (mevcut != null)
            {
                _ctx.BirimRateler.Remove(mevcut);
            }
        }

        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Özel fiyatlar güncellendi.";
        return RedirectToAction(nameof(OzelFiyat), new { id });
    }
}

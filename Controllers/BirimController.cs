using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Birim")]
public class BirimController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly IRezervasyonService _rezervasyonService;
    private readonly ITarifeHiyerarsiService _tarifeHiyerarsisi;
    private readonly IBirimService _birimService;

    public BirimController(ApplicationDbContext ctx, IRezervasyonService rezervasyonService, ITarifeHiyerarsiService tarifeHiyerarsisi, IBirimService birimService)
    {
        _ctx = ctx;
        _rezervasyonService = rezervasyonService;
        _tarifeHiyerarsisi = tarifeHiyerarsisi;
        _birimService = birimService;
    }

    [Authorize(Policy = PermissionCatalog.Birim.ManageRate)]
    [HttpGet("{id:int}/OzelFiyat")]
    public async Task<IActionResult> OzelFiyat(int id)
    {
        var birim = await _birimService.GetByIdAsync(id);

        if (birim == null) return NotFound();

        var vm = new BirimOzelFiyatViewModel
        {
            BirimId = birim.Id,
            BirimAd = birim.Ad,
            TasinmazId = birim.TasinmazId,
            TasinmazAd = birim.TasinmazAd,
            KiralanabilirMi = birim.KiralanabilirMi,
            RezervasyonYapilabilirMi = birim.RezervasyonYapilabilirMi,
            BirimTuruAd = birim.BirimTuruAd
        };

        if (vm.KiralanabilirMi)
        {
            vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(
                TarifeHiyerarsiKatmani.Birim, tasinmazId: birim.TasinmazId, yil: DateTime.Now.Year);

            var aktifBorcTipleri = await _ctx.BorcTipleri
                .Where(b => b.Aktif && b.Davranis != BorcTipiDavranisi.KullaniciManuel && b.Davranis != BorcTipiDavranisi.RezervasyonOzel)
                .OrderBy(b => b.Sira)
                .ToListAsync();

            var kategoriler = await _ctx.Kategoriler
                .Where(k => k.Tipi == KategoriTipi.Kiraci && k.Aktif)
                .OrderBy(k => k.Sira)
                .ToListAsync();

            var mevcutRateler = await _ctx.BirimTarifeler
                .Where(r => r.BirimId == id)
                .ToListAsync();

            vm.Kolonlar = aktifBorcTipleri.Select(bt => new BirimTarifeKolonu
            {
                BorcTipiId = bt.Id,
                BorcTipiAd = bt.Ad,
                BorcTipiKod = bt.Kod,
                BorcTipiDavranisi = bt.Davranis
            }).ToList();

            vm.Satirlar = kategoriler.Select(kat => new BirimTarifeKategoriSatiri
            {
                KiraciKategoriId = kat.Id,
                KiraciKategoriAd = kat.Ad,
                Hucreler = aktifBorcTipleri.Select(bt =>
                {
                    var rate = mevcutRateler.FirstOrDefault(r =>
                        r.KiraciKategoriId == kat.Id && r.BorcTipiId == bt.Id);
                    return new BirimTarifeHucre
                    {
                        RateId = rate?.Id ?? 0,
                        KiraciKategoriId = kat.Id,
                        BorcTipiId = bt.Id,
                        OzelFiyatAktif = rate != null,
                        HesaplamaYontemi = rate?.HesaplamaYontemi ?? HesaplamaYontemi.Sabit,
                        BirimDeger = rate?.BirimDeger ?? 0,
                        KdvOrani = rate?.KdvOrani ?? 0
                    };
                }).ToList()
            }).ToList();
        }
        else if (vm.RezervasyonYapilabilirMi)
        {
            vm.OzelRezervasyonKural = await _ctx.RezervasyonTarifeler
                .FirstOrDefaultAsync(r => r.BirimId == id);

            vm.ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(DateTime.Now.Year);
        }

        return View(vm);
    }

    [Authorize(Policy = PermissionCatalog.Birim.ManageRate)]
    [HttpPost("{id:int}/OzelFiyat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OzelFiyat(int id, BirimOzelFiyatViewModel vm)
    {
        var mevcutRateler = await _ctx.BirimTarifeler
            .Where(r => r.BirimId == id)
            .ToListAsync();

        foreach (var satir in vm.Satirlar)
        {
            foreach (var hucre in satir.Hucreler)
            {
                var mevcut = mevcutRateler.FirstOrDefault(r =>
                    r.KiraciKategoriId == hucre.KiraciKategoriId &&
                    r.BorcTipiId == hucre.BorcTipiId);

                if (hucre.OzelFiyatAktif)
                {
                    if (mevcut == null)
                    {
                        _ctx.BirimTarifeler.Add(new BirimTarife
                        {
                            BirimId = id,
                            KiraciKategoriId = hucre.KiraciKategoriId,
                            BorcTipiId = hucre.BorcTipiId,
                            HesaplamaYontemi = hucre.HesaplamaYontemi,
                            BirimDeger = hucre.BirimDeger,
                            KdvOrani = hucre.KdvOrani
                        });
                    }
                    else
                    {
                        mevcut.HesaplamaYontemi = hucre.HesaplamaYontemi;
                        mevcut.BirimDeger = hucre.BirimDeger;
                        mevcut.KdvOrani = hucre.KdvOrani;
                    }
                }
                else if (mevcut != null)
                {
                    _ctx.BirimTarifeler.Remove(mevcut);
                }
            }
        }

        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Özel fiyatlar güncellendi.";
        return RedirectToAction(nameof(OzelFiyat), new { id });
    }

    [Authorize(Policy = PermissionCatalog.Birim.ManageRate)]
    [HttpPost("{id:int}/RezKuralKaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RezKuralKaydet(int id, RezervasyonTarifeKuralViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Form alanlarını kontrol edin.";
            return RedirectToAction(nameof(OzelFiyat), new { id });
        }
        vm.BirimId = id;
        var (basarili, hata, _) = await _rezervasyonService.SaveUcretKuralAsync(vm);
        TempData[basarili ? "Success" : "Error"] = basarili
            ? "Özel rezervasyon kuralı kaydedildi."
            : hata;
        return RedirectToAction(nameof(OzelFiyat), new { id });
    }

    [Authorize(Policy = PermissionCatalog.Birim.ManageRate)]
    [HttpPost("{id:int}/RezKuralSifirla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RezKuralSifirla(int id)
    {
        var kural = await _ctx.RezervasyonTarifeler
            .FirstOrDefaultAsync(r => r.BirimId == id);
        if (kural != null)
        {
            _ctx.RezervasyonTarifeler.Remove(kural);
            await _ctx.SaveChangesAsync();
        }
        TempData["Success"] = "Özel kural kaldırıldı. Genel tarife uygulanacak.";
        return RedirectToAction(nameof(OzelFiyat), new { id });
    }
}

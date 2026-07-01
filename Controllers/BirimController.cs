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
    private readonly IYetkiKapsamiProvider _provider;

    public BirimController(ApplicationDbContext ctx, IRezervasyonService rezervasyonService, ITarifeHiyerarsiService tarifeHiyerarsisi, IBirimService birimService, IYetkiKapsamiProvider provider)
    {
        _ctx = ctx;
        _rezervasyonService = rezervasyonService;
        _tarifeHiyerarsisi = tarifeHiyerarsisi;
        _birimService = birimService;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Birim.OverrideRate)]
    [HttpGet("{id:int}/OzelFiyat")]
    public async Task<IActionResult> OzelFiyat(int id)
    {
        var birim = await _birimService.GetByIdAsync(id);

        if (birim == null) return NotFound();

        if (!_provider.KapsamdaMi(birim.TasinmazId)) return Forbid();

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

            var tasinmazFiyatlar = await _ctx.TasinmazTarifeler
                .Where(f => f.TasinmazId == birim.TasinmazId)
                .ToListAsync();

            var genelFiyatlar = await _ctx.GenelTarifeler
                .Where(g => g.Yil == DateTime.Now.Year)
                .ToListAsync();

            // Yürürlükteki tüm üst tarifeleri tek bir listede topla
            var parentTarifeSatirlar = new List<ParentTarifeSatir>();
            foreach (var kat in kategoriler)
            {
                foreach (var bt in aktifBorcTipleri)
                {
                    var tasinmazRate = tasinmazFiyatlar.FirstOrDefault(tf => tf.KiraciKategoriId == kat.Id && tf.BorcTipiId == bt.Id);
                    var genelRate = genelFiyatlar.FirstOrDefault(gf => gf.KiraciKategoriId == kat.Id && gf.BorcTipiId == bt.Id);

                    decimal deger = 0;
                    decimal kdv = 0;
                    HesaplamaYontemi yontem = (bt.Kod == Models.Entities.BorcTipiConsts.Kira ? HesaplamaYontemi.M2 : HesaplamaYontemi.Sabit);
                    string kaynak = "Tanımsız";

                    if (tasinmazRate != null)
                    {
                        deger = tasinmazRate.BirimDeger;
                        kdv = tasinmazRate.KdvOrani;
                        yontem = tasinmazRate.HesaplamaYontemi;
                        kaynak = "Taşınmaz Tarifesi";
                    }
                    else if (genelRate != null)
                    {
                        deger = genelRate.BirimDeger;
                        kdv = genelRate.KdvOrani;
                        yontem = genelRate.HesaplamaYontemi;
                        kaynak = "Genel Tarife";
                    }
                    else
                    {
                        continue;
                    }

                    parentTarifeSatirlar.Add(new ParentTarifeSatir
                    {
                        KategoriAd = kat.Ad,
                        BorcTipiAd = bt.Ad,
                        HesaplamaYontemi = yontem,
                        BirimDeger = deger,
                        KdvOrani = kdv,
                        Kaynak = kaynak
                    });
                }
            }

            vm.ParentTarife = new ParentTarifeKartViewModel
            {
                KaynakAdi = "Yürürlükteki Üst Tarifeler (Varsayılanlar)",
                Aciklama = "Taşınmaz ve Genel Tarifelerin birleşimi",
                Satirlar = parentTarifeSatirlar
            };

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

                    var tasinmazRate = tasinmazFiyatlar.FirstOrDefault(tf => tf.KiraciKategoriId == kat.Id && tf.BorcTipiId == bt.Id);
                    var genelRate = genelFiyatlar.FirstOrDefault(gf => gf.KiraciKategoriId == kat.Id && gf.BorcTipiId == bt.Id);

                    decimal varsayilanDeger = 0;
                    decimal varsayilanKdv = 0;
                    HesaplamaYontemi varsayilanYontem = (bt.Kod == Models.Entities.BorcTipiConsts.Kira ? HesaplamaYontemi.M2 : HesaplamaYontemi.Sabit);
                    string kaynak = "Tanımsız";

                    if (tasinmazRate != null)
                    {
                        varsayilanDeger = tasinmazRate.BirimDeger;
                        varsayilanKdv = tasinmazRate.KdvOrani;
                        varsayilanYontem = tasinmazRate.HesaplamaYontemi;
                        kaynak = "Taşınmaz Tarifesi";
                    }
                    else if (genelRate != null)
                    {
                        varsayilanDeger = genelRate.BirimDeger;
                        varsayilanKdv = genelRate.KdvOrani;
                        varsayilanYontem = genelRate.HesaplamaYontemi;
                        kaynak = "Genel Tarife";
                    }

                    return new BirimTarifeHucre
                    {
                        RateId = rate?.Id ?? 0,
                        KiraciKategoriId = kat.Id,
                        BorcTipiId = bt.Id,
                        OzelFiyatAktif = rate != null,
                        HesaplamaYontemi = rate?.HesaplamaYontemi ?? (bt.Kod == Models.Entities.BorcTipiConsts.Kira ? HesaplamaYontemi.M2 : HesaplamaYontemi.Sabit),
                        BirimDeger = rate?.BirimDeger ?? 0,
                        KdvOrani = rate?.KdvOrani ?? 0,
                        VarsayilanBirimDeger = varsayilanDeger,
                        VarsayilanKdvOrani = varsayilanKdv,
                        VarsayilanHesaplamaYontemi = varsayilanYontem,
                        VarsayilanKaynak = kaynak
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

    [Authorize(Policy = PermissionCatalog.Birim.OverrideRate)]
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

    [Authorize(Policy = PermissionCatalog.Birim.OverrideRate)]
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

    [Authorize(Policy = PermissionCatalog.Birim.OverrideRate)]
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

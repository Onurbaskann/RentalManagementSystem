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
[Route("Unit")]
public class UnitController : Controller
{
    private readonly ApplicationDbContext _ctx;
    private readonly IReservationService _reservationService;
    private readonly IRateHierarchyService _tarifeHiyerarsisi;
    private readonly IUnitService _birimService;
    private readonly IPermissionScopeProvider _provider;

    public UnitController(ApplicationDbContext ctx, IReservationService rezervasyonService, IRateHierarchyService tarifeHiyerarsisi, IUnitService unitService, IPermissionScopeProvider provider)
    {
        _ctx = ctx;
        _reservationService = rezervasyonService;
        _tarifeHiyerarsisi = tarifeHiyerarsisi;
        _birimService = unitService;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpGet("{id:int}/OzelFiyat")]
    public async Task<IActionResult> OzelFiyat(int id)
    {
        var unit = await _birimService.GetByIdAsync(id);

        if (unit == null) return NotFound();

        if (!_provider.KapsamdaMi(unit.TasinmazId)) return Forbid();

        var vm = new BirimOzelFiyatViewModel
        {
            BirimId = unit.Id,
            BirimAd = unit.Ad,
            TasinmazId = unit.TasinmazId,
            TasinmazAd = unit.TasinmazAd,
            KiralanabilirMi = unit.KiralanabilirMi,
            RezervasyonYapilabilirMi = unit.RezervasyonYapilabilirMi,
            UnitTypeAd = unit.UnitTypeAd
        };

        if (vm.KiralanabilirMi)
        {
            var aktifBorcTipleri = await _ctx.ChargeTypes
                .Where(b => b.IsActive && b.Behavior != ChargeTypeBehavior.UserManual && b.Behavior != ChargeTypeBehavior.ReservationSpecific)
                .OrderBy(b => b.SortOrder)
                .ToListAsync();

            var kategoriler = await _ctx.Kategoriler
                .Where(k => k.Tipi == KategoriTipi.Tenant && k.IsActive)
                .OrderBy(k => k.Sira)
                .ToListAsync();

            var mevcutRateler = await _ctx.BirimTarifeler
                .Where(r => r.UnitId == id)
                .ToListAsync();

            var tasinmazFiyatlar = await _ctx.TasinmazTarifeler
                .Where(f => f.PropertyId == unit.TasinmazId)
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
                    var tasinmazRate = tasinmazFiyatlar.FirstOrDefault(tf => tf.KiraciKategoriId == kat.Id && tf.ChargeTypeId == bt.Id);
                    var genelRate = genelFiyatlar.FirstOrDefault(gf => gf.KiraciKategoriId == kat.Id && gf.ChargeTypeId == bt.Id);

                    decimal deger = 0;
                    decimal kdv = 0;
                    CalculationMethod yontem = (bt.Code == Models.Entities.BorcTipiConsts.Kira ? CalculationMethod.M2 : CalculationMethod.Fixed);
                    string kaynak = "Tanımsız";

                    if (tasinmazRate != null)
                    {
                        deger = tasinmazRate.UnitValue;
                        kdv = tasinmazRate.KdvRate;
                        yontem = tasinmazRate.CalculationMethod;
                        kaynak = "Taşınmaz Tarifesi";
                    }
                    else if (genelRate != null)
                    {
                        deger = genelRate.UnitValue;
                        kdv = genelRate.KdvRate;
                        yontem = genelRate.CalculationMethod;
                        kaynak = "Genel Tarife";
                    }
                    else
                    {
                        continue;
                    }

                    parentTarifeSatirlar.Add(new ParentTarifeSatir
                    {
                        KategoriAd = kat.Ad,
                        ChargeTypeName = bt.Name,
                        CalculationMethod = yontem,
                        UnitValue = deger,
                        KdvRate = kdv,
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
                ChargeTypeId = bt.Id,
                ChargeTypeName = bt.Name,
                ChargeTypeCode = bt.Code,
                ChargeTypeBehavior = bt.Behavior
            }).ToList();

            vm.Satirlar = kategoriler.Select(kat => new BirimTarifeKategoriSatiri
            {
                KiraciKategoriId = kat.Id,
                KiraciKategoriAd = kat.Ad,
                Hucreler = aktifBorcTipleri.Select(bt =>
                {
                    var rate = mevcutRateler.FirstOrDefault(r =>
                        r.KiraciKategoriId == kat.Id && r.ChargeTypeId == bt.Id);

                    var tasinmazRate = tasinmazFiyatlar.FirstOrDefault(tf => tf.KiraciKategoriId == kat.Id && tf.ChargeTypeId == bt.Id);
                    var genelRate = genelFiyatlar.FirstOrDefault(gf => gf.KiraciKategoriId == kat.Id && gf.ChargeTypeId == bt.Id);

                    decimal varsayilanDeger = 0;
                    decimal varsayilanKdv = 0;
                    CalculationMethod varsayilanYontem = (bt.Code == Models.Entities.BorcTipiConsts.Kira ? CalculationMethod.M2 : CalculationMethod.Fixed);
                    string kaynak = "Tanımsız";

                    if (tasinmazRate != null)
                    {
                        varsayilanDeger = tasinmazRate.UnitValue;
                        varsayilanKdv = tasinmazRate.KdvRate;
                        varsayilanYontem = tasinmazRate.CalculationMethod;
                        kaynak = "Taşınmaz Tarifesi";
                    }
                    else if (genelRate != null)
                    {
                        varsayilanDeger = genelRate.UnitValue;
                        varsayilanKdv = genelRate.KdvRate;
                        varsayilanYontem = genelRate.CalculationMethod;
                        kaynak = "Genel Tarife";
                    }

                    return new BirimTarifeHucre
                    {
                        RateId = rate?.Id ?? 0,
                        KiraciKategoriId = kat.Id,
                        ChargeTypeId = bt.Id,
                        OzelFiyatAktif = rate != null,
                        CalculationMethod = rate?.CalculationMethod ?? (bt.Code == Models.Entities.BorcTipiConsts.Kira ? CalculationMethod.M2 : CalculationMethod.Fixed),
                        UnitValue = rate?.UnitValue ?? 0,
                        KdvRate = rate?.KdvRate ?? 0,
                        VarsayilanBirimDeger = varsayilanDeger,
                        VarsayilanKdvOrani = varsayilanKdv,
                        VarsayilanCalculationMethod = varsayilanYontem,
                        VarsayilanKaynak = kaynak
                    };
                }).ToList()
            }).ToList();
        }
        else if (vm.RezervasyonYapilabilirMi)
        {
            vm.OzelRezervasyonKural = await _ctx.RezervasyonTarifeler
                .FirstOrDefaultAsync(r => r.UnitId == id);

            vm.ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(DateTime.Now.Year);
        }

        return View(vm);
    }

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpPost("{id:int}/OzelFiyat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OzelFiyat(int id, BirimOzelFiyatViewModel vm)
    {
        var mevcutRateler = await _ctx.BirimTarifeler
            .Where(r => r.UnitId == id)
            .ToListAsync();

        foreach (var satir in vm.Satirlar)
        {
            foreach (var hucre in satir.Hucreler)
            {
                var mevcut = mevcutRateler.FirstOrDefault(r =>
                    r.KiraciKategoriId == hucre.KiraciKategoriId &&
                    r.ChargeTypeId == hucre.ChargeTypeId);

                if (hucre.OzelFiyatAktif)
                {
                    if (mevcut == null)
                    {
                        _ctx.BirimTarifeler.Add(new BirimTarife
                        {
                            UnitId = id,
                            KiraciKategoriId = hucre.KiraciKategoriId,
                            ChargeTypeId = hucre.ChargeTypeId,
                            CalculationMethod = hucre.CalculationMethod,
                            UnitValue = hucre.UnitValue,
                            KdvRate = hucre.KdvRate
                        });
                    }
                    else
                    {
                        mevcut.CalculationMethod = hucre.CalculationMethod;
                        mevcut.UnitValue = hucre.UnitValue;
                        mevcut.KdvRate = hucre.KdvRate;
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

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
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
        var (basarili, hata, _) = await _reservationService.SaveUcretKuralAsync(vm);
        TempData[basarili ? "Success" : "Error"] = basarili
            ? "Özel reservation kuralı kaydedildi."
            : hata;
        return RedirectToAction(nameof(OzelFiyat), new { id });
    }

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpPost("{id:int}/RezKuralSifirla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RezKuralSifirla(int id)
    {
        var kural = await _ctx.RezervasyonTarifeler
            .FirstOrDefaultAsync(r => r.UnitId == id);
        if (kural != null)
        {
            _ctx.RezervasyonTarifeler.Remove(kural);
            await _ctx.SaveChangesAsync();
        }
        TempData["Success"] = "Özel kural kaldırıldı. Genel tarife uygulanacak.";
        return RedirectToAction(nameof(OzelFiyat), new { id });
    }
}

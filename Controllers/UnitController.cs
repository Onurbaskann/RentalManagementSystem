using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
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
    [HttpGet("{id}/OzelFiyat")]
    public async Task<IActionResult> OzelFiyat(int id)
    {
        var unit = await _birimService.GetByIdAsync(id);

        if (unit == null) return NotFound();

        if (!_provider.IsInScope(unit.PropertyId)) return Forbid();

        var vm = new BirimOzelFiyatViewModel
        {
            UnitId = unit.Id,
            UnitName = unit.Name,
            PropertyId = unit.PropertyId,
            PropertyName = unit.PropertyName,
            IsLeasable = unit.CanBeRented,
            IsReservable = unit.CanBeReserved,
            UnitTypeName = unit.UnitTypeName
        };

        if (vm.IsLeasable)
        {
            var aktifBorcTipleri = await _ctx.ChargeTypes
                .Where(b => b.IsActive && b.Behavior != ChargeTypeBehavior.UserManual && b.Behavior != ChargeTypeBehavior.ReservationSpecific)
                .OrderBy(b => b.SortOrder)
                .ToListAsync();

            var kategoriler = await _ctx.Kategoriler
                .Where(k => k.Type == CategoryType.Tenant && k.IsActive)
                .OrderBy(k => k.Order)
                .ToListAsync();

            var mevcutRateler = await _ctx.UnitRates
                .Where(r => r.UnitId == id)
                .ToListAsync();

            var tasinmazFiyatlar = await _ctx.TasinmazTarifeler
                .Where(f => f.PropertyId == unit.PropertyId)
                .ToListAsync();

            var genelFiyatlar = await _ctx.GenelTarifeler
                .Where(g => g.Year == DateTime.Now.Year)
                .ToListAsync();

            // Yürürlükteki tüm üst tarifeleri tek bir listede topla
            var parentTarifeSatirlar = new List<ParentTarifeSatir>();
            foreach (var kat in kategoriler)
            {
                foreach (var bt in aktifBorcTipleri)
                {
                    var tasinmazRate = tasinmazFiyatlar.FirstOrDefault(tf => tf.TenantCategoryId == kat.Id && tf.ChargeTypeId == bt.Id);
                    var genelRate = genelFiyatlar.FirstOrDefault(gf => gf.TenantCategoryId == kat.Id && gf.ChargeTypeId == bt.Id);

                    decimal deger = 0;
                    decimal kdv = 0;
                    CalculationMethod yontem = (bt.Code == Models.Constants.BorcTipiConsts.Kira ? CalculationMethod.M2 : CalculationMethod.Fixed);
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
                        CategoryName = kat.Name,
                        ChargeTypeName = bt.Name,
                        CalculationMethod = yontem,
                        UnitValue = deger,
                        KdvRate = kdv,
                        Source = kaynak
                    });
                }
            }

            vm.ParentTarife = new ParentTarifeKartViewModel
            {
                SourceName = "Yürürlükteki Üst Tarifeler (Varsayılanlar)",
                Description = "Taşınmaz ve Genel Tarifelerin birleşimi",
                Rows = parentTarifeSatirlar
            };

            vm.Columns = aktifBorcTipleri.Select(bt => new UnitRateColumn
            {
                ChargeTypeId = bt.Id,
                ChargeTypeName = bt.Name,
                ChargeTypeCode = bt.Code,
                ChargeTypeBehavior = bt.Behavior
            }).ToList();

            vm.Rows = kategoriler.Select(kat => new UnitRateCategoryRow
            {
                TenantCategoryId = kat.Id,
                TenantCategoryName = kat.Name,
                Hucreler = aktifBorcTipleri.Select(bt =>
                {
                    var rate = mevcutRateler.FirstOrDefault(r =>
                        r.TenantCategoryId == kat.Id && r.ChargeTypeId == bt.Id);

                    var tasinmazRate = tasinmazFiyatlar.FirstOrDefault(tf => tf.TenantCategoryId == kat.Id && tf.ChargeTypeId == bt.Id);
                    var genelRate = genelFiyatlar.FirstOrDefault(gf => gf.TenantCategoryId == kat.Id && gf.ChargeTypeId == bt.Id);

                    decimal varsayilanDeger = 0;
                    decimal varsayilanKdv = 0;
                    CalculationMethod varsayilanYontem = (bt.Code == Models.Constants.BorcTipiConsts.Kira ? CalculationMethod.M2 : CalculationMethod.Fixed);
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

                    return new UnitRateCell
                    {
                        RateId = rate?.Id ?? 0,
                        TenantCategoryId = kat.Id,
                        ChargeTypeId = bt.Id,
                        IsCustomRateActive = rate != null,
                        CalculationMethod = rate?.CalculationMethod ?? (bt.Code == Models.Constants.BorcTipiConsts.Kira ? CalculationMethod.M2 : CalculationMethod.Fixed),
                        UnitValue = rate?.UnitValue ?? 0,
                        KdvRate = rate?.KdvRate ?? 0,
                        DefaultUnitValue = varsayilanDeger,
                        DefaultKdvRate = varsayilanKdv,
                        DefaultCalculationMethod = varsayilanYontem,
                        DefaultSource = kaynak
                    };
                }).ToList()
            }).ToList();
        }
        else if (vm.IsReservable)
        {
            vm.OzelRezervasyonKural = await _ctx.RezervasyonTarifeler
                .FirstOrDefaultAsync(r => r.UnitId == id);

            vm.ParentReservationRateOverride = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(DateTime.Now.Year);
        }

        return View(vm);
    }

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpPost("{id}/OzelFiyat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OzelFiyat(int id, BirimOzelFiyatViewModel vm)
    {
        var mevcutRateler = await _ctx.UnitRates
            .Where(r => r.UnitId == id)
            .ToListAsync();

        foreach (var satir in vm.Rows)
        {
            foreach (var hucre in satir.Hucreler)
            {
                var mevcut = mevcutRateler.FirstOrDefault(r =>
                    r.TenantCategoryId == hucre.TenantCategoryId &&
                    r.ChargeTypeId == hucre.ChargeTypeId);

                if (hucre.IsCustomRateActive)
                {
                    if (mevcut == null)
                    {
                        _ctx.UnitRates.Add(new UnitRate
                        {
                            UnitId = id,
                            TenantCategoryId = hucre.TenantCategoryId,
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
                    _ctx.UnitRates.Remove(mevcut);
                }
            }
        }

        await _ctx.SaveChangesAsync();
        TempData["Success"] = "Özel fiyatlar güncellendi.";
        return RedirectToAction(nameof(OzelFiyat), new { id });
    }

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpPost("{id}/RezKuralKaydet")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RezKuralKaydet(int id, ReservationRateOverrideViewModel vm)
    {
        vm.UnitId = id;
        var (basarili, hata, _) = await _reservationService.SaveUcretKuralAsync(vm);
        TempData[basarili ? "Success" : "Error"] = basarili
            ? "Özel reservation kuralı kaydedildi."
            : hata;
        return RedirectToAction(nameof(OzelFiyat), new { id });
    }

    [Authorize(Policy = PermissionCatalog.Unit.OverrideRate)]
    [HttpPost("{id}/RezKuralSifirla")]
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

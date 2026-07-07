using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize]
public class PropertyController : Controller
{
    private readonly IPropertyService _tasinmazService;
    private readonly IPropertyPricingService _tasinmazFiyatService;
    private readonly IStatisticsService _istatistik;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _ctx;
    private readonly IRateHierarchyService _tarifeHiyerarsisi;
    private readonly IPermissionScopeProvider _provider;

    public PropertyController(
        IPropertyService propertyService,
        IPropertyPricingService propertyPricingService,
        IStatisticsService istatistik,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext ctx,
        IRateHierarchyService tarifeHiyerarsisi,
        IPermissionScopeProvider provider)
    {
        _tasinmazService = propertyService;
        _tasinmazFiyatService = propertyPricingService;
        _istatistik = istatistik;
        _userManager = userManager;
        _ctx = ctx;
        _tarifeHiyerarsisi = tarifeHiyerarsisi;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Property.Module)]
    public async Task<IActionResult> Index()
    {
        var tasinmazlar = await _tasinmazService.GetAllAsync(_provider.GlobalAccess ? null : _provider.AccessiblePropertyIds);
        return View(tasinmazlar);
    }

    [Authorize(Policy = PermissionCatalog.Property.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        if (!_provider.IsInScope(id)) return Forbid();

        var t = await _tasinmazService.GetByIdAsync(id);
        if (t == null) return NotFound();

        var vm = new TasinmazDetayViewModel
        {
            Property = t,
            FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(id, pageSize: 100)
        };
        return View(vm);
    }

    private async Task PopulateViewBagAsync()
    {
        var birimTurleri = await _ctx.UnitTypes.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ToListAsync();
        ViewBag.BirimTurleri = birimTurleri.Where(b => b.CanBeRented).ToList();
        ViewBag.RezervasyonBirimTurleri = birimTurleri.Where(b => b.CanBeReserved).ToList();
        var tipler = await _ctx.TasinmazTipleri.Where(k => k.IsActive).OrderBy(t => t.Sira).ToListAsync();
        ViewBag.TasinmazTipleri = tipler;
        ViewBag.TasinmazTipiKiralamaSekilleri = tipler.ToDictionary(
            t => t.Id,
            t =>
            {
                var list = new List<int>();
                if (t.TekParcaDestekli) list.Add((int)RentalMode.WholeProperty);
                if (t.BirimBazliDestekli) list.Add((int)RentalMode.UnitBased);
                return list.ToArray();
            });
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Property.Create)]
    public async Task<IActionResult> Ekle()
    {
        await PopulateViewBagAsync();
        var vm = new TasinmazEkleViewModel
        {
            FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(0, pageSize: 100),
            ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Property, yil: DateTime.Now.Year),
            ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year)
        };
        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Property.Edit)]
    public async Task<IActionResult> Duzenle(int id)
    {
        if (!_provider.IsInScope(id)) return Forbid();

        var vm = await _tasinmazService.GetForEditAsync(id);
        if (vm == null) return NotFound();

        vm.FiyatMatrisi = await _tasinmazFiyatService.GetMatrisiAsync(id, pageSize: 100);
        vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Property, yil: DateTime.Now.Year);
        vm.ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

        await PopulateViewBagAsync();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Property.Edit)]
    public async Task<IActionResult> Duzenle(TasinmazDuzenleViewModel vm)
    {
        _provider.PropertyGuard(vm.Id);

        if (string.IsNullOrWhiteSpace(vm.Ad))
            ModelState.AddModelError("Ad", "Taşınmaz adı zorunludur.");
        if (vm.TasinmazTipiId == null || vm.TasinmazTipiId <= 0)
            ModelState.AddModelError("TasinmazTipiId", "Taşınmaz tipi zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Il))
            ModelState.AddModelError("Il", "İl zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Ilce))
            ModelState.AddModelError("Ilce", "İlçe zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Mahalle))
            ModelState.AddModelError("Mahalle", "Mahalle zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.AcikAdres))
            ModelState.AddModelError("AcikAdres", "Açık adres zorunludur.");

        if (vm.RentalMode == RentalMode.UnitBased)
        {
            if (vm.Units == null || vm.Units.Count == 0)
            {
                ModelState.AddModelError("Units", "Unit bazlı kiralama için en az bir unit eklemelisiniz.");
            }
            else
            {
                for (int i = 0; i < vm.Units.Count; i++)
                {
                    var unit = vm.Units[i];
                    if (string.IsNullOrWhiteSpace(unit.BirimNo))
                        ModelState.AddModelError($"Units[{i}].BirimNo", "Unit No zorunludur.");
                    if (unit.KatNo == null)
                        ModelState.AddModelError($"Units[{i}].KatNo", "Kat No zorunludur.");
                    if (unit.UnitTypeId == null || unit.UnitTypeId <= 0)
                        ModelState.AddModelError($"Units[{i}].UnitTypeId", "Unit Türü zorunludur.");
                    if (string.IsNullOrWhiteSpace(unit.Ad) && !string.IsNullOrWhiteSpace(unit.BirimNo))
                        unit.Ad = "Unit " + unit.BirimNo;
                    if (string.IsNullOrWhiteSpace(unit.Ad))
                        ModelState.AddModelError($"Units[{i}].Ad", "Ad zorunludur.");
                    if (unit.Yuzolcumu <= 0)
                        ModelState.AddModelError($"Units[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
                }

                var tekrarlayanBirimNo = vm.Units
                    .Where(b => !string.IsNullOrWhiteSpace(b.BirimNo))
                    .GroupBy(b => b.BirimNo.Trim().ToUpper())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (tekrarlayanBirimNo != null)
                    ModelState.AddModelError("Units", $"Unit No '{tekrarlayanBirimNo}' aynı taşınmaz içinde tekrar kullanılamaz.");
            }
        }

        for (int i = 0; i < vm.RezervasyonAlanlari.Count; i++)
        {
            var alan = vm.RezervasyonAlanlari[i];
            if (string.IsNullOrWhiteSpace(alan.BirimNo))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].BirimNo", "Unit No zorunludur.");
            if (string.IsNullOrWhiteSpace(alan.Ad))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Ad", "Alan Adı zorunludur.");
            if (alan.Yuzolcumu <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
            if (alan.UnitTypeId == null || alan.UnitTypeId <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].UnitTypeId", "Alan Türü zorunludur.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync();
            var freshMatris = await _tasinmazFiyatService.GetMatrisiAsync(vm.Id, pageSize: 100);
            if (vm.FiyatMatrisi?.Satirlar != null)
            {
                foreach (var freshSatir in freshMatris.Satirlar)
                {
                    var userSatir = vm.FiyatMatrisi.Satirlar.FirstOrDefault(s => s.KiraciKategoriId == freshSatir.KiraciKategoriId);
                    if (userSatir != null)
                    {
                        foreach (var freshHucre in freshSatir.Hucreler)
                        {
                            var userHucre = userSatir.Hucreler.FirstOrDefault(h => h.ChargeTypeId == freshHucre.ChargeTypeId);
                            if (userHucre != null)
                            {
                                freshHucre.UnitValue = userHucre.UnitValue;
                                freshHucre.CalculationMethod = userHucre.CalculationMethod;
                                freshHucre.KdvRate = userHucre.KdvRate;
                            }
                        }
                    }
                }
            }
            vm.FiyatMatrisi = freshMatris;

            vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Property, yil: DateTime.Now.Year);
            vm.ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

            // AktifSozlesmesiVar flag'leri POST round-trip'te sıfırlanıyor, DB'den tekrar doldur
            await RestoreAktifFlaglarAsync(vm);

            return View(vm);
        }

        await _tasinmazService.UpdateWithChildrenAsync(vm);

        var userId2 = _userManager.GetUserId(User);
        await _tasinmazFiyatService.SaveMatrisiAsync(vm.Id, vm.FiyatMatrisi, userId2 ?? "");

        TempData["Success"] = $"'{vm.Ad}' başarıyla güncellendi.";
        return RedirectToAction("Detay", new { id = vm.Id });
    }

    private async Task RestoreAktifFlaglarAsync(TasinmazDuzenleViewModel vm)
    {
        var now = DateTime.Now;
        foreach (var b in vm.Units.Where(b => b.Id.HasValue))
        {
            b.AktifSozlesmesiVar = await _ctx.Leases
                .AnyAsync(s => s.UnitId == b.Id!.Value
                               && s.Status == LeaseStatus.Active
                               && s.StartDate <= now
                               && s.EndDate >= now);
        }

        var rezIds = vm.RezervasyonAlanlari.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToList();
        var aktifRezBirimIds = await _ctx.Reservations
            .Where(r => rezIds.Contains(r.UnitId)
                        && r.Status == ReservationStatus.Planned
                        && r.EndDate >= now)
            .Select(r => r.UnitId)
            .Distinct()
            .ToListAsync();

        foreach (var r in vm.RezervasyonAlanlari.Where(r => r.Id.HasValue))
            r.AktifRezervasyonuVar = aktifRezBirimIds.Contains(r.Id!.Value);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Property.Create)]
    public async Task<IActionResult> Ekle(TasinmazEkleViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Ad))
            ModelState.AddModelError("Ad", "Taşınmaz adı zorunludur.");
        if (vm.TasinmazTipiId == null || vm.TasinmazTipiId <= 0)
            ModelState.AddModelError("TasinmazTipiId", "Taşınmaz tipi zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Il))
            ModelState.AddModelError("Il", "İl zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Ilce))
            ModelState.AddModelError("Ilce", "İlçe zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.Mahalle))
            ModelState.AddModelError("Mahalle", "Mahalle zorunludur.");
        if (string.IsNullOrWhiteSpace(vm.AcikAdres))
            ModelState.AddModelError("AcikAdres", "Açık adres zorunludur.");

        if (vm.TasinmazTipiId != null && vm.TasinmazTipiId > 0)
        {
            var tip = await _ctx.TasinmazTipleri.FirstOrDefaultAsync(k => k.Id == vm.TasinmazTipiId.Value);
            if (tip != null)
            {
                var secimIzinli = vm.RentalMode == RentalMode.WholeProperty ? tip.TekParcaDestekli : tip.BirimBazliDestekli;
                if (!secimIzinli)
                    ModelState.AddModelError("RentalMode", "Seçilen taşınmaz tipi bu kiralama şekline izin vermiyor.");
            }
        }

        if (vm.RentalMode == RentalMode.UnitBased)
        {
            if (vm.Units == null || vm.Units.Count == 0)
            {
                ModelState.AddModelError("Units", "Unit bazlı kiralama için en az bir unit eklemelisiniz.");
            }
            else
            {
                for (int i = 0; i < vm.Units.Count; i++)
                {
                    var unit = vm.Units[i];

                    if (string.IsNullOrWhiteSpace(unit.BirimNo))
                        ModelState.AddModelError($"Units[{i}].BirimNo", "Unit No zorunludur.");

                    if (unit.KatNo == null)
                        ModelState.AddModelError($"Units[{i}].KatNo", "Kat No zorunludur.");

                    if (unit.UnitTypeId == null || unit.UnitTypeId <= 0)
                        ModelState.AddModelError($"Units[{i}].UnitTypeId", "Unit Türü zorunludur.");

                    if (string.IsNullOrWhiteSpace(unit.Ad) && !string.IsNullOrWhiteSpace(unit.BirimNo))
                        unit.Ad = "Unit " + unit.BirimNo;
                    if (string.IsNullOrWhiteSpace(unit.Ad))
                        ModelState.AddModelError($"Units[{i}].Ad", "Ad zorunludur.");

                    if (unit.Yuzolcumu <= 0)
                        ModelState.AddModelError($"Units[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
                }

                var tekrarlayanBirimNo = vm.Units
                    .Where(b => !string.IsNullOrWhiteSpace(b.BirimNo))
                    .GroupBy(b => b.BirimNo.Trim().ToUpper())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (tekrarlayanBirimNo != null)
                    ModelState.AddModelError("Units", $"Unit No '{tekrarlayanBirimNo}' aynı taşınmaz içinde tekrar kullanılamaz.");
            }
        }
        else
        {
            vm.Units.Clear();
        }

        for (int i = 0; i < vm.RezervasyonAlanlari.Count; i++)
        {
            var alan = vm.RezervasyonAlanlari[i];
            if (string.IsNullOrWhiteSpace(alan.BirimNo))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].BirimNo", "Unit No zorunludur.");
            if (string.IsNullOrWhiteSpace(alan.Ad))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Ad", "Alan Adı zorunludur.");
            if (alan.Yuzolcumu <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
            if (alan.UnitTypeId == null || alan.UnitTypeId <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].UnitTypeId", "Alan Türü zorunludur.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateViewBagAsync();

            var freshMatris = await _tasinmazFiyatService.GetMatrisiAsync(0, pageSize: 100);
            if (vm.FiyatMatrisi?.Satirlar != null)
            {
                foreach (var freshSatir in freshMatris.Satirlar)
                {
                    var userSatir = vm.FiyatMatrisi.Satirlar.FirstOrDefault(s => s.KiraciKategoriId == freshSatir.KiraciKategoriId);
                    if (userSatir != null)
                    {
                        foreach (var freshHucre in freshSatir.Hucreler)
                        {
                            var userHucre = userSatir.Hucreler.FirstOrDefault(h => h.ChargeTypeId == freshHucre.ChargeTypeId);
                            if (userHucre != null)
                            {
                                freshHucre.UnitValue = userHucre.UnitValue;
                                freshHucre.CalculationMethod = userHucre.CalculationMethod;
                                freshHucre.KdvRate = userHucre.KdvRate;
                            }
                        }
                    }
                }
            }
            vm.FiyatMatrisi = freshMatris;

            vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(TarifeHiyerarsiKatmani.Property, yil: DateTime.Now.Year);
            vm.ParentRezervasyonTarife = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

            return View(vm);
        }

        var t = new Property
        {
            Name = vm.Ad,
            PropertyTypeId = vm.TasinmazTipiId,
            RentalMode = vm.RentalMode,
            City = vm.Il,
            District = vm.Ilce,
            Neighborhood = vm.Mahalle,
            Address = vm.AcikAdres,
            OpenArea = vm.AcikYuzolcumu,
            ClosedArea = vm.KapaliYuzolcumu,
            FloorCount = vm.KatSayisi,
            Description = vm.Aciklama
        };

        await _tasinmazService.CreateAsync(t,
            vm.Units.Count > 0 ? vm.Units : null,
            vm.RezervasyonAlanlari.Count > 0 ? vm.RezervasyonAlanlari : null);

        // Fiyat Matrisini Kaydet
        var userId = _userManager.GetUserId(User);
        await _tasinmazFiyatService.SaveMatrisiAsync(t.Id, vm.FiyatMatrisi, userId ?? "");

        TempData["Success"] = $"'{t.Name}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = t.Id });
    }
}

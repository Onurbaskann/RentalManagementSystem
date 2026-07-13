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
        ViewBag.TumBirimTurleri = birimTurleri;
        ViewBag.BirimTurleri = birimTurleri.Where(b => b.Usage != UnitTypeUsage.Reservable).ToList();
        ViewBag.RezervasyonBirimTurleri = birimTurleri.Where(b => b.Usage == UnitTypeUsage.Reservable).ToList();
        var tipler = await _ctx.TasinmazTipleri.Where(k => k.IsActive).OrderBy(t => t.SortOrder).ToListAsync();
        ViewBag.TasinmazTipleri = tipler;
        ViewBag.TasinmazTipiBirimYapilari = tipler.ToDictionary(
            t => t.Id,
            t =>
            {
                var list = new List<int>();
                if (t.SupportsSingleUnit) list.Add((int)UnitStructure.SingleUnit);
                if (t.SupportsMultipleUnits) list.Add((int)UnitStructure.MultipleUnits);
                return list.ToArray();
            });
    }

    private async Task ValidateUnitTypesAsync(
        UnitStructure structure,
        int? singleUnitTypeId,
        IEnumerable<int?> normalUnitTypeIds,
        IEnumerable<int?> reservationUnitTypeIds)
    {
        if (structure == UnitStructure.SingleUnit)
        {
            if (singleUnitTypeId is > 0 && !await _ctx.UnitTypes.AnyAsync(t => t.Id == singleUnitTypeId && t.IsActive))
                ModelState.AddModelError("KompleUnitTypeId", "Seçilen birim türü aktif değil veya bulunamadı.");
            return;
        }

        var normalIds = normalUnitTypeIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (normalIds.Count > 0 && await _ctx.UnitTypes.AnyAsync(t => normalIds.Contains(t.Id) && t.Usage == UnitTypeUsage.Reservable))
            ModelState.AddModelError("Units", "Rezervasyon türündeki birimler rezervasyon alanları bölümünden eklenmelidir.");

        var reservationIds = reservationUnitTypeIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (reservationIds.Count > 0 && await _ctx.UnitTypes.AnyAsync(t => reservationIds.Contains(t.Id) && t.Usage != UnitTypeUsage.Reservable))
            ModelState.AddModelError("RezervasyonAlanlari", "Rezervasyon alanları yalnızca rezervasyon türündeki birim türlerini kullanabilir.");
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
            ParentReservationRateOverride = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year)
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
        vm.ParentReservationRateOverride = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

        await PopulateViewBagAsync();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Property.Edit)]
    public async Task<IActionResult> Duzenle([FromForm] TasinmazDuzenleViewModel vm)
    {
        _provider.PropertyGuard(vm.Id);

        var mevcutTasinmaz = await _ctx.Properties
            .AsNoTracking()
            .Include(t => t.Units)
            .FirstOrDefaultAsync(t => t.Id == vm.Id);
        if (mevcutTasinmaz == null) return NotFound();

        vm.BirimYapisiDegistirilebilir = await _tasinmazService.CanChangeUnitStructureAsync(vm.Id);
        if (vm.UnitStructure != mevcutTasinmaz.UnitStructure && !vm.BirimYapisiDegistirilebilir)
        {
            ModelState.AddModelError("UnitStructure", "Sözleşme, rezervasyon veya tahakkuk geçmişi bulunan taşınmazın birim yapısı değiştirilemez.");
            vm.UnitStructure = mevcutTasinmaz.UnitStructure;
        }

        if (vm.TasinmazTipiId is > 0)
        {
            var tip = await _ctx.TasinmazTipleri
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == vm.TasinmazTipiId.Value);
            if (tip != null)
            {
                var secimIzinli = vm.UnitStructure == UnitStructure.SingleUnit
                    ? tip.SupportsSingleUnit
                    : tip.SupportsMultipleUnits;
                if (!secimIzinli)
                    ModelState.AddModelError("UnitStructure", "Seçilen taşınmaz tipi bu birim yapısına izin vermiyor.");
            }
        }
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

        if (vm.UnitStructure == UnitStructure.MultipleUnits)
        {
            if ((vm.Units?.Count ?? 0) + (vm.RezervasyonAlanlari?.Count ?? 0) == 0)
            {
                ModelState.AddModelError("Units", "Çoklu birim yapısı için en az bir birim eklemelisiniz.");
            }
            else
            {
                for (int i = 0; i < vm.Units.Count; i++)
                {
                    var unit = vm.Units[i];
                    if (string.IsNullOrWhiteSpace(unit.UnitNo))
                        ModelState.AddModelError($"Units[{i}].BirimNo", "Birim No zorunludur.");
                    if (unit.FloorNo == null)
                        ModelState.AddModelError($"Units[{i}].KatNo", "Kat No zorunludur.");
                    if (unit.UnitTypeId == null || unit.UnitTypeId <= 0)
                        ModelState.AddModelError($"Units[{i}].UnitTypeId", "Birim Türü zorunludur.");
                    if (string.IsNullOrWhiteSpace(unit.Name) && !string.IsNullOrWhiteSpace(unit.UnitNo))
                        unit.Name = "Birim " + unit.UnitNo;
                    if (string.IsNullOrWhiteSpace(unit.Name))
                        ModelState.AddModelError($"Units[{i}].Ad", "Ad zorunludur.");
                    if (unit.Area <= 0)
                        ModelState.AddModelError($"Units[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
                }

                var tekrarlayanBirimNo = vm.Units
                    .Where(b => !string.IsNullOrWhiteSpace(b.UnitNo))
                    .GroupBy(b => b.UnitNo.Trim().ToUpper())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (tekrarlayanBirimNo != null)
                    ModelState.AddModelError("Units", $"Birim No '{tekrarlayanBirimNo}' aynı taşınmaz içinde tekrar kullanılamaz.");
            }
        }
        else
        {
            if (vm.KompleUnitTypeId is null or <= 0)
                ModelState.AddModelError("KompleUnitTypeId", "Tek birim yapısı için birim türü zorunludur.");
            vm.Units.Clear();
            vm.RezervasyonAlanlari.Clear();
        }

        for (int i = 0; i < vm.RezervasyonAlanlari.Count; i++)
        {
            var alan = vm.RezervasyonAlanlari[i];
            if (string.IsNullOrWhiteSpace(alan.UnitNo))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].BirimNo", "Birim No zorunludur.");
            if (string.IsNullOrWhiteSpace(alan.Name))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Ad", "Alan Adı zorunludur.");
            if (alan.Area <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
            if (alan.UnitTypeId == null || alan.UnitTypeId <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].UnitTypeId", "Alan Türü zorunludur.");
        }

        await ValidateUnitTypesAsync(vm.UnitStructure, vm.KompleUnitTypeId,
            vm.Units.Select(u => u.UnitTypeId),
            vm.RezervasyonAlanlari.Select(r => r.UnitTypeId));

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
            vm.ParentReservationRateOverride = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

            // AktifSozlesmesiVar flag'leri POST round-trip'te sıfırlanıyor, DB'den tekrar doldur
            await RestoreAktifFlaglarAsync(vm);

            return View(vm);
        }

        // Silinen birimlerde kayıtlı rezervasyon kontrolü
        var databaseBirimler = await _ctx.Units
            .Where(b => b.PropertyId == vm.Id)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();

        var gelenBirimIds = vm.Units?.Where(u => u.Id.HasValue).Select(u => u.Id!.Value).ToHashSet() ?? new HashSet<int>();
        var gelenRezIds = vm.RezervasyonAlanlari?.Where(a => a.Id.HasValue).Select(a => a.Id!.Value).ToHashSet() ?? new HashSet<int>();

        foreach (var dbBirim in databaseBirimler)
        {
            if (!gelenBirimIds.Contains(dbBirim.Id) && !gelenRezIds.Contains(dbBirim.Id))
            {
                var hasReservations = await _ctx.Reservations.AnyAsync(r => r.UnitId == dbBirim.Id);
                if (hasReservations)
                {
                    ModelState.AddModelError("Units", $"Silinmeye çalışılan '{dbBirim.Name}' biriminin/alanının kayıtlı rezervasyonları bulunmaktadır. Rezervasyonu olan bir birim silinemez.");
                }
            }
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
            vm.ParentReservationRateOverride = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

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
                var secimIzinli = vm.UnitStructure == UnitStructure.SingleUnit ? tip.SupportsSingleUnit : tip.SupportsMultipleUnits;
                if (!secimIzinli)
                    ModelState.AddModelError("UnitStructure", "Seçilen taşınmaz tipi bu birim yapısına izin vermiyor.");
            }
        }

        if (vm.UnitStructure == UnitStructure.MultipleUnits)
        {
            if ((vm.Units?.Count ?? 0) + (vm.RezervasyonAlanlari?.Count ?? 0) == 0)
            {
                ModelState.AddModelError("Units", "Çoklu birim yapısı için en az bir birim eklemelisiniz.");
            }
            else
            {
                for (int i = 0; i < vm.Units.Count; i++)
                {
                    var unit = vm.Units[i];

                    if (string.IsNullOrWhiteSpace(unit.UnitNo))
                        ModelState.AddModelError($"Units[{i}].BirimNo", "Birim No zorunludur.");

                    if (unit.FloorNo == null)
                        ModelState.AddModelError($"Units[{i}].KatNo", "Kat No zorunludur.");

                    if (unit.UnitTypeId == null || unit.UnitTypeId <= 0)
                        ModelState.AddModelError($"Units[{i}].UnitTypeId", "Birim Türü zorunludur.");

                    if (string.IsNullOrWhiteSpace(unit.Name) && !string.IsNullOrWhiteSpace(unit.UnitNo))
                        unit.Name = "Birim " + unit.UnitNo;
                    if (string.IsNullOrWhiteSpace(unit.Name))
                        ModelState.AddModelError($"Units[{i}].Ad", "Ad zorunludur.");

                    if (unit.Area <= 0)
                        ModelState.AddModelError($"Units[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
                }

                var tekrarlayanBirimNo = vm.Units
                    .Where(b => !string.IsNullOrWhiteSpace(b.UnitNo))
                    .GroupBy(b => b.UnitNo.Trim().ToUpper())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .FirstOrDefault();

                if (tekrarlayanBirimNo != null)
                    ModelState.AddModelError("Units", $"Birim No '{tekrarlayanBirimNo}' aynı taşınmaz içinde tekrar kullanılamaz.");
            }
        }
        else
        {
            if (vm.KompleUnitTypeId is null or <= 0)
                ModelState.AddModelError("KompleUnitTypeId", "Tek birim yapısı için birim türü zorunludur.");
            vm.Units.Clear();
            vm.RezervasyonAlanlari.Clear();
        }

        for (int i = 0; i < vm.RezervasyonAlanlari.Count; i++)
        {
            var alan = vm.RezervasyonAlanlari[i];
            if (string.IsNullOrWhiteSpace(alan.UnitNo))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].BirimNo", "Birim No zorunludur.");
            if (string.IsNullOrWhiteSpace(alan.Name))
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Ad", "Alan Adı zorunludur.");
            if (alan.Area <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].Yuzolcumu", "Yüzölçümü 0'dan büyük olmalıdır.");
            if (alan.UnitTypeId == null || alan.UnitTypeId <= 0)
                ModelState.AddModelError($"RezervasyonAlanlari[{i}].UnitTypeId", "Alan Türü zorunludur.");
        }

        await ValidateUnitTypesAsync(vm.UnitStructure, vm.KompleUnitTypeId,
            vm.Units.Select(u => u.UnitTypeId),
            vm.RezervasyonAlanlari.Select(r => r.UnitTypeId));

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
            vm.ParentReservationRateOverride = await _tarifeHiyerarsisi.GetRezervasyonParentForAsync(yil: DateTime.Now.Year);

            return View(vm);
        }

        var t = new Property
        {
            Name = vm.Ad,
            PropertyTypeId = vm.TasinmazTipiId,
            UnitStructure = vm.UnitStructure,
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
            vm.RezervasyonAlanlari.Count > 0 ? vm.RezervasyonAlanlari : null,
            vm.KompleUnitTypeId);

        // Fiyat Matrisini Kaydet
        var userId = _userManager.GetUserId(User);
        await _tasinmazFiyatService.SaveMatrisiAsync(t.Id, vm.FiyatMatrisi, userId ?? "");

        TempData["Success"] = $"'{t.Name}' başarıyla eklendi.";
        return RedirectToAction("Detay", new { id = t.Id });
    }
}

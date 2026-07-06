using KiraTakip.Authorization;
using KiraTakip.Extensions;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Settings;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KiraTakip.Controllers;

[Authorize]
public class SozlesmeController : Controller
{
    private readonly ILeaseService _sozlesmeService;
    private readonly IPropertyService _tasinmazService;
    private readonly ITenantService _kiraciService;
    private readonly IStatisticsService _istatistik;
    private readonly IChargeService _chargeService;
    private readonly IChargeGenerationService _chargeGeneration;
    private readonly ApplicationDbContext _ctx;
    private readonly IPermissionScopeProvider _provider;
    private readonly IRateHierarchyService _tarifeHiyerarsisi;
    private readonly IMailService _mail;
    private readonly IPaymentLinkService _paymentLink;
    private readonly IRazorViewToStringRenderer _razorRenderer;
    private readonly IOptions<PaymentLinkSettings> _paymentLinkOptions;
    private readonly ILogger<SozlesmeController> _logger;
    private readonly IDocumentService _belgeService;

    public SozlesmeController(
        ILeaseService leaseService,
        IPropertyService propertyService,
        ITenantService tenantService,
        IStatisticsService istatistik,
        IChargeService tahakkukService,
        IChargeGenerationService tahakkukUretim,
        ApplicationDbContext ctx,
        IRateHierarchyService tarifeHiyerarsisi,
        IMailService mail,
        IPaymentLinkService paymentLink,
        IRazorViewToStringRenderer razorRenderer,
        IOptions<PaymentLinkSettings> paymentLinkOptions,
        ILogger<SozlesmeController> logger,
        IPermissionScopeProvider provider,
        IDocumentService documentService)
    {
        _sozlesmeService = leaseService;
        _tasinmazService = propertyService;
        _kiraciService = tenantService;
        _istatistik = istatistik;
        _chargeService = tahakkukService;
        _chargeGeneration = tahakkukUretim;
        _ctx = ctx;
        _tarifeHiyerarsisi = tarifeHiyerarsisi;
        _mail = mail;
        _paymentLink = paymentLink;
        _razorRenderer = razorRenderer;
        _paymentLinkOptions = paymentLinkOptions;
        _logger = logger;
        _provider = provider;
        _belgeService = documentService;
    }

    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> Index(string? filtre)
    {
        var sozlesmeler = await _sozlesmeService.GetAllAsync(filtre, _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds);

        var now = DateTime.Today;
        var esik = now.AddDays(_paymentLinkOptions.Value.ReminderDaysBefore);
        var borcluSayisi = await _ctx.Charges
            .Where(t => t.DueDate <= esik
                && t.Status != ChargeStatus.Paid
                && t.Status != ChargeStatus.Cancelled
                && t.Lease != null
                && t.Lease.TenantId != 0)
            .GroupBy(t => t.Lease!.TenantId)
            .CountAsync();
        ViewBag.BorcluSayisi = borcluSayisi;

        ViewBag.Filtre = filtre ?? "tum";
        return View(sozlesmeler);
    }

    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        if (!_provider.KapsamdaMi(s.TasinmazId)) return Forbid();

        var gecmis = await _sozlesmeService.GetByUnitIdAsync(s.BirimId);
        var kiraciSozlesmeleri = await _sozlesmeService.GetByTenantIdAsync(s.KiraciId);

        var dummySozlesme = new Lease
        {
            Id = s.Id,
            TenantId = s.KiraciId,
            UnitId = s.BirimId,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Durum,
            TerminationDate = s.FesihTarihi,
            Unit = new Unit
            {
                Id = s.BirimId,
                Area = s.BirimYuzolcumu,
                UnitKind = s.UnitKind,
                PropertyId = s.TasinmazId
            }
        };

        var vm = new SozlesmeDetayViewModel
        {
            Lease = s,
            KalanGun = _istatistik.KalanGun(dummySozlesme),
            AylikBedel = await _istatistik.AylikBedelAsync(dummySozlesme),
            YillikBedel = await _istatistik.YillikBedelAsync(dummySozlesme),
            Aktif = _istatistik.Aktif(dummySozlesme),
            SureYuzdesi = _istatistik.SureYuzdesi(dummySozlesme),
            Durum = _istatistik.GetBirimDurumu(dummySozlesme.Unit),
            GecmisSozlesmeler = gecmis.Where(x => x.Id != id).ToList(),
            KiraciSozlesmeleri = kiraciSozlesmeleri.Where(x => x.Id != id && x.BirimId != s.BirimId).ToList(),
            KdvOraniEtkin = s.SozlesmeTarifeler
                .FirstOrDefault(r => r.BorcTipiDavranis == ChargeTypeBehavior.MonthlyFixed)?.KdvRate ?? 20m
        };

        var hasRegeneratePermission = User.HasPermission(PermissionCatalog.Charge.Regenerate);
        if (User.HasPermission(PermissionCatalog.Payment.Module) || hasRegeneratePermission)
        {
            vm.HasOdemeAccess = User.HasPermission(PermissionCatalog.Payment.Module);
            await _chargeService.GecikmeleriGuncelleAsync();
            vm.Charges = await _chargeService.GetListAsync(leaseId: id);
        }

        if (vm.Charges != null && vm.Charges.Any())
        {
            var ilkOdenmemis = vm.Charges
                .Where(t => t.SourceType == ChargeSourceType.Lease 
                            && t.Durum != ChargeStatus.Paid 
                            && t.PaidAmount == 0)
                .OrderBy(t => t.PeriodStart)
                .FirstOrDefault();

            if (ilkOdenmemis != null)
            {
                vm.DefaultYenidenUretBaslangicTarihi = ilkOdenmemis.PeriodStart;
            }
            else
            {
                vm.DefaultYenidenUretBaslangicTarihi = DateTime.Today;
            }

            var sonOdenen = vm.Charges
                .Where(t => t.SourceType == ChargeSourceType.Lease 
                            && (t.Durum == ChargeStatus.Paid || t.PaidAmount > 0))
                .OrderByDescending(t => t.PeriodStart)
                .FirstOrDefault();

            if (sonOdenen != null)
            {
                vm.SonOdenenDonem = sonOdenen.PeriodStart;
            }

            vm.OdenmemisTahakkukSayisi = vm.Charges
                .Count(t => t.SourceType == ChargeSourceType.Lease 
                            && t.Durum != ChargeStatus.Paid 
                            && t.PaidAmount == 0);
        }
        else
        {
            vm.DefaultYenidenUretBaslangicTarihi = DateTime.Today;
        }

        var bugun = DateTime.Today;
        var guncelTahakkuk = await _ctx.Charges
            .Include(t => t.LineItems).ThenInclude(k => k.ChargeType)
            .Where(t => t.LeaseId == id && t.Status != ChargeStatus.Cancelled && t.PeriodStart <= bugun)
            .OrderByDescending(t => t.PeriodStart)
            .FirstOrDefaultAsync();
        vm.GuncelKalemler = guncelTahakkuk?.LineItems
            .Where(k => k.ChargeType.Behavior == ChargeTypeBehavior.MonthlyFixed)
            .OrderBy(k => k.ChargeType.SortOrder).ToList() ?? new();
        vm.GuncelKalemDonemi = guncelTahakkuk?.PeriodStart;

        vm.ParentTarife = await _tarifeHiyerarsisi.GetParentForAsync(
            TarifeHiyerarsiKatmani.Lease,
            propertyId: s.TasinmazId,
            unitId: s.BirimId,
            kategoriId: s.KiraciKategoriId,
            yil: s.StartDate.Year);

        var depozitoTutarlari = await _sozlesmeService.GetDepozitoTutarlariAsync(new[] { id });
        vm.DepozitoTutari = depozitoTutarlari.TryGetValue(id, out var dep) ? dep : null;

        vm.Belgeler    = await _belgeService.GetListAsync(BelgeOwnerTipi.Lease, id);
        vm.DocumentTypes = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Lease);

        var manuelBorclar = await _ctx.Charges
            .Where(t => t.LeaseId == id
                     && t.SourceType == ChargeSourceType.Manual
                     && t.Status != ChargeStatus.Cancelled)
            .Select(t => new { Kalan = t.TotalAmount - t.PaidAmount })
            .ToListAsync();
        if (manuelBorclar.Count > 0)
        {
            ViewBag.ManuelBorcSayisi = manuelBorclar.Count;
            ViewBag.ManuelBorcKalan  = manuelBorclar.Sum(x => x.Kalan);
        }

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Lease.Create)]
    public async Task<IActionResult> Ekle(int? unitId)
    {
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync();
        var kiraciler = await _kiraciService.GetAllAsync();
        ViewBag.BirimYuzolcumular = System.Text.Json.JsonSerializer.Serialize(
            bosBirimler.ToDictionary(b => b.Id, b => (double)b.Yuzolcumu));
        var vm = new SozlesmeEkleViewModel
        {
            BirimId = unitId,
            MevcutBirimler = bosBirimler,
            Tenants = kiraciler,
            DocumentTypes = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Lease)
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Create)]
    public async Task<IActionResult> Ekle(SozlesmeEkleViewModel vm)
    {
        vm.MevcutBirimler = await _tasinmazService.GetBosBirimlerAsync();
        vm.Tenants = await _kiraciService.GetAllAsync();
        vm.DocumentTypes = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Lease);

        if (vm.BirimId == null || vm.BirimId == 0)
            ModelState.AddModelError("BirimId", "Lütfen bir birim seçin.");

        if (vm.EndDate <= vm.StartDate)
            ModelState.AddModelError("EndDate", "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.");

        if (vm.VadeGunu < 1 || vm.VadeGunu > 31)
            ModelState.AddModelError("VadeGunu", "Vade günü 1-31 arasında olmalıdır.");

        foreach (var bt in vm.DocumentTypes.Where(bt => bt.Required))
        {
            var f = Request.Form.Files.GetFile($"dosya_{bt.Id}");
            if (f == null || f.Length == 0)
                ModelState.AddModelError($"dosya_{bt.Id}", $"'{bt.Name}' belgesi zorunludur.");
        }

        if (!ModelState.IsValid) return View(vm);

        var kiraKalemi = vm.SozlesmeKalemleri
            .FirstOrDefault(k => k.Davranis == ChargeTypeBehavior.MonthlyFixed);

        var kdvUygulanacakMi = kiraKalemi != null && kiraKalemi.KdvRate > 0;
        var kdvOrani = kdvUygulanacakMi ? kiraKalemi!.KdvRate : 0;
        var kiraBedeli = vm.SozlesmeKalemleri
            .Where(k => k.Davranis == ChargeTypeBehavior.MonthlyFixed)
            .Sum(k => k.Amount);

        var s = new Lease
        {
            UnitId = vm.BirimId!.Value,
            TenantId = vm.KiraciId,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            Description = vm.Aciklama,
            Status = LeaseStatus.Active,
            IsKdvApplied = kdvUygulanacakMi,
            DueDateRuleType = vm.DueDateRuleType,
            DueDay = vm.VadeGunu,
        };

        await _sozlesmeService.CreateAsync(s, kiraBedeli);

        // Override kalemlerini kaydet
        if (vm.SozlesmeKalemleri != null && vm.SozlesmeKalemleri.Any())
        {
            foreach (var k in vm.SozlesmeKalemleri.Where(x => x.KullaniciDegistirdiMi))
            {
                var rate = new SozlesmeTarife
                {
                    LeaseId = s.Id,
                    ChargeTypeId = k.ChargeTypeId,
                    UnitValue = k.UnitValue,
                    CalculationMethod = k.CalculationMethod,
                    KdvRate = k.KdvRate
                };
                _ctx.SozlesmeTarifeler.Add(rate);
            }
            await _ctx.SaveChangesAsync();
        }

        await _chargeGeneration.UretSozlesmeIcinAsync(s.Id);

        foreach (var bt in vm.DocumentTypes)
        {
            var file = Request.Form.Files.GetFile($"dosya_{bt.Id}");
            if (file == null || file.Length == 0) continue;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            await _belgeService.UploadAsync(
                BelgeOwnerTipi.Lease, s.Id, bt.Id,
                file.FileName, file.ContentType, ms.ToArray());
        }

        TempData["Success"] = "Sözleşme başarıyla oluşturuldu.";
        return RedirectToAction("Detay", new { id = s.Id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Extend)]
    public async Task<IActionResult> Uzat(int id, SozlesmeUzatViewModel vm)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        if (s.Durum == LeaseStatus.Terminated)
        {
            TempData["Error"] = "Feshedilmiş sözleşme uzatılamaz.";
            return RedirectToAction("Detay", new { id });
        }

        if (vm.YeniBitisTarihi <= s.EndDate)
            ModelState.AddModelError("YeniBitisTarihi", "Yeni bitiş tarihi mevcut bitiş tarihinden büyük olmalıdır.");

        if (vm.TufeUygulanacakMi && vm.TufeOrani.HasValue && vm.TufeOrani.Value < 0)
            ModelState.AddModelError("TufeOrani", "TÜFE oranı negatif olamaz.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction("Detay", new { id });
        }

        var dummySozlesme = new Lease
        {
            Id = s.Id,
            TenantId = s.KiraciId,
            UnitId = s.BirimId,
            Unit = new Unit { Id = s.BirimId, Area = s.BirimYuzolcumu }
        };
        var eskiBedel = await _istatistik.AylikBedelAsync(dummySozlesme);

        if (vm.TarifeyiGuncelle && vm.SozlesmeKalemleri != null && vm.SozlesmeKalemleri.Any())
        {
            var eskiRateler = await _ctx.SozlesmeTarifeler.Where(r => r.LeaseId == id).ToListAsync();
            _ctx.SozlesmeTarifeler.RemoveRange(eskiRateler);
            foreach (var k in vm.SozlesmeKalemleri.Where(x => x.KullaniciDegistirdiMi))
            {
                _ctx.SozlesmeTarifeler.Add(new SozlesmeTarife
                {
                    LeaseId = id,
                    ChargeTypeId = k.ChargeTypeId,
                    UnitValue = k.UnitValue,
                    CalculationMethod = k.CalculationMethod,
                    KdvRate = k.KdvRate
                });
            }
            await _ctx.SaveChangesAsync();
        }

        var yeniRateler = await _ctx.SozlesmeTarifeler
            .Include(r => r.ChargeType)
            .Where(r => r.LeaseId == id).ToListAsync();
        var yeniBedel = HesaplaAylikBedelHelper(yeniRateler, s.BirimYuzolcumu);

        await _sozlesmeService.UzatAsync(id, vm.YeniBitisTarihi, eskiBedel, yeniBedel,
            vm.KdvUygulanacakMi, vm.KdvRate ?? 20, vm.TufeOrani, vm.Aciklama);
        await _chargeGeneration.UretSozlesmeIcinAsync(id);

        TempData["Success"] = "Sözleşme süresi başarıyla uzatıldı.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Edit)]
    public async Task<IActionResult> VadeGuncelle(int id, DueDateRuleType vadeKuraliTipi, int vadeGunu, string? aciklama)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        if (s.Durum == LeaseStatus.Terminated)
        {
            TempData["Error"] = "Feshedilmiş sözleşmenin vadesi güncellenemez.";
            return RedirectToAction("Detay", new { id });
        }

        if (vadeGunu < 1 || vadeGunu > 31)
        {
            TempData["Error"] = "Vade günü 1-31 arasında olmalıdır.";
            return RedirectToAction("Detay", new { id });
        }

        await _sozlesmeService.VadeGuncelleAsync(id, vadeKuraliTipi, vadeGunu, aciklama);
        await _chargeGeneration.BekleyenVadeleriYenidenHesaplaAsync(id);

        TempData["Success"] = "Vade kuralı güncellendi ve bekleyen tahakkuklar yenilendi.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Terminate)]
    public async Task<IActionResult> Feshet(int id, SozlesmeFesihViewModel vm)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        if (s.Durum == LeaseStatus.Terminated)
        {
            TempData["Error"] = "Sözleşme zaten feshedilmiş.";
            return RedirectToAction("Detay", new { id });
        }

        if (string.IsNullOrWhiteSpace(vm.FesihNedeni))
            ModelState.AddModelError("FesihNedeni", "Fesih nedeni zorunludur.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction("Detay", new { id });
        }

        await _sozlesmeService.FeshetAsync(id, vm.FesihTarihi, vm.FesihNedeni, vm.Aciklama);
        await _chargeGeneration.IptalEtFutureTahakkuklarAsync(id, vm.FesihTarihi);
        TempData["Success"] = "Sözleşme başarıyla feshedildi.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Charge.Regenerate)]
    public async Task<IActionResult> YenidenUret(int id, DateTime baslangicTarihi,
        bool tarifeyiGuncelle = false, List<SozlesmeKalemInputDto>? sozlesmeKalemleri = null)
    {
        var s = await _ctx.Leases
            .Include(x => x.ActivityLog)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();

        if (tarifeyiGuncelle && sozlesmeKalemleri != null && sozlesmeKalemleri.Any())
        {
            var eskiRateler = await _ctx.SozlesmeTarifeler.Where(r => r.LeaseId == id).ToListAsync();
            _ctx.SozlesmeTarifeler.RemoveRange(eskiRateler);
            foreach (var k in sozlesmeKalemleri.Where(x => x.KullaniciDegistirdiMi))
            {
                _ctx.SozlesmeTarifeler.Add(new SozlesmeTarife
                {
                    LeaseId = id,
                    ChargeTypeId = k.ChargeTypeId,
                    UnitValue = k.UnitValue,
                    CalculationMethod = k.CalculationMethod,
                    KdvRate = k.KdvRate
                });
            }
            await _ctx.SaveChangesAsync();
        }

        await _chargeGeneration.YenidenUretAsync(id, baslangicTarihi);

        s.ActivityLog.Add(new SozlesmeIslemGecmisi
        {
            LeaseId = id,
            IslemTipi = LeaseActivityType.ChargeRegeneration,
            TransactionDate = DateTime.Now,
            Aciklama = $"{baslangicTarihi:MMMM yyyy} tarihinden itibaren ödenmemiş tahakkuklar yeniden üretildi."
                       + (tarifeyiGuncelle ? " (Tarife güncellendi.)" : "")
        });

        await _ctx.SaveChangesAsync();
        TempData["Success"] = $"{baslangicTarihi:MMMM yyyy} tarihinden itibaren ödenmemiş tahakkuklar yeniden üretildi.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Edit)]
    public IActionResult HesaplaTufeKdv(decimal mevcutBedel, decimal? tufeOrani, bool kdvUygulanacakMi, decimal? kdvOrani)
    {
        var sonuc = _istatistik.HesaplaKiraArtisi(mevcutBedel, tufeOrani, kdvUygulanacakMi, kdvOrani);
        return Json(sonuc);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetVarsayilanKalemler(int unitId, int tenantId, DateTime baslangic, int? leaseId = null)
    {
        var previews = await _chargeGeneration.ComposeKalemlerAsync(unitId, tenantId, baslangic, leaseId);
        var result = previews.Select(p => new SozlesmeKalemInputDto
        {
            ChargeTypeId = p.ChargeTypeId,
            ChargeTypeName = p.ChargeTypeName,
            ChargeTypeCode = p.ChargeTypeCode,
            Davranis = p.Davranis,
            VarsayilanTutar = p.Amount,
            Amount = p.Amount,
            UnitValue = p.UnitValue,
            VarsayilanBirimDeger = p.UnitValue,
            KdvRate = p.KdvRate,
            CalculationMethod = p.CalculationMethod,
            SourceType = p.SourceType.ToString(),
            RateBulundu = p.RateBulundu,
            KullaniciDegistirdiMi = false
        }).ToList();

        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> BorclularaMailGonder([FromServices] IChargeReminderService chargeReminderService)
    {
        try
        {
            var sonuc = await chargeReminderService.GonderAsync();
            var mesajParcalari = new List<string>();
            if (sonuc.BasariliGonderim > 0) mesajParcalari.Add($"{sonuc.BasariliGonderim} kiracıya e-posta gönderildi");
            if (sonuc.CooldownAtlanan > 0) mesajParcalari.Add($"{sonuc.CooldownAtlanan} kiracı (bekleme süresinde olduğu için) atlandı");
            if (sonuc.BasarisizGonderim > 0) mesajParcalari.Add($"{sonuc.BasarisizGonderim} gönderimde hata oluştu");
            if (mesajParcalari.Count == 0) mesajParcalari.Add("Gönderilecek tahakkuk bulunamadı");

            if (sonuc.BasarisizGonderim > 0)
                TempData["Error"] = string.Join(", ", mesajParcalari) + ". Detaylar için logları inceleyin.";
            else
                TempData["Success"] = string.Join(", ", mesajParcalari) + ".";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu hatırlatma işlemi sırasında beklenmeyen hata.");
            TempData["Error"] = "Beklenmeyen bir hata oluştu. Detaylar için logları inceleyin.";
        }

        return RedirectToAction("Index");
    }

    private static decimal HesaplaAylikBedelHelper(IEnumerable<SozlesmeTarife> rates, decimal yuzolcumu) =>
        rates.Where(r => r.ChargeType?.Behavior == ChargeTypeBehavior.MonthlyFixed)
             .Sum(r => r.CalculationMethod == CalculationMethod.M2 ? r.UnitValue * yuzolcumu : r.UnitValue);
}

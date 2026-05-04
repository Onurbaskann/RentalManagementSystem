using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Controllers;

[Authorize]
public class SozlesmeController : Controller
{
    private readonly ISozlesmeService _sozlesmeService;
    private readonly ITasinmazService _tasinmazService;
    private readonly IKiraciService _kiraciService;
    private readonly IIstatistikService _istatistik;
    private readonly UserManager<ApplicationUser> _userManager;

    public SozlesmeController(
        ISozlesmeService sozlesmeService,
        ITasinmazService tasinmazService,
        IKiraciService kiraciService,
        IIstatistikService istatistik,
        UserManager<ApplicationUser> userManager)
    {
        _sozlesmeService = sozlesmeService;
        _tasinmazService = tasinmazService;
        _kiraciService = kiraciService;
        _istatistik = istatistik;
        _userManager = userManager;
    }

    [Authorize(Policy = PermissionCatalog.Sozlesme.View)]
    public async Task<IActionResult> Index(string? filtre)
    {
        var userId = _userManager.GetUserId(User);
        var filterUserId = User.IsInRole("Goruntuleyici") ? userId : null;
        var sozlesmeler = await _sozlesmeService.GetAllAsync(filtre, filterUserId);

        ViewBag.Filtre = filtre ?? "tum";
        return View(sozlesmeler);
    }

    [Authorize(Policy = PermissionCatalog.Sozlesme.View)]
    public async Task<IActionResult> Detay(int id)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User);
            var tasinmazlar = await _tasinmazService.GetAllAsync(userId);
            if (!tasinmazlar.Any(t => t.Id == s.Birim.TasinmazId)) return Forbid();
        }

        var gecmis = await _sozlesmeService.GetByBirimIdAsync(s.BirimId);

        var vm = new SozlesmeDetayViewModel
        {
            Sozlesme = s,
            KalanGun = _istatistik.KalanGun(s),
            AylikBedel = _istatistik.AylikBedel(s),
            YillikBedel = _istatistik.YillikBedel(s),
            Aktif = _istatistik.Aktif(s),
            SureYuzdesi = _istatistik.SureYuzdesi(s),
            Durum = _istatistik.GetBirimDurumu(s.Birim),
            GecmisSozlesmeler = gecmis.Where(x => x.Id != id).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Create)]
    public async Task<IActionResult> Ekle(int? birimId)
    {
        var bosBirimler = await _tasinmazService.GetBosBirimlerAsync();
        var kiraciler = await _kiraciService.GetAllAsync();
        var vm = new SozlesmeEkleViewModel
        {
            BirimId = birimId,
            MevcutBirimler = bosBirimler,
            Kiraciler = kiraciler
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Create)]
    public async Task<IActionResult> Ekle(SozlesmeEkleViewModel vm)
    {
        vm.MevcutBirimler = await _tasinmazService.GetBosBirimlerAsync();
        vm.Kiraciler = await _kiraciService.GetAllAsync();

        if (vm.BirimId == null || vm.BirimId == 0)
            ModelState.AddModelError("BirimId", "Lütfen bir birim seçin.");

        if (vm.BitisTarihi <= vm.BaslangicTarihi)
            ModelState.AddModelError("BitisTarihi", "Bitiş tarihi başlangıç tarihinden büyük olmalıdır.");

        if (!ModelState.IsValid) return View(vm);

        var s = new KiraSozlesmesi
        {
            BirimId = vm.BirimId!.Value,
            KiraciId = vm.KiraciId,
            BaslangicTarihi = vm.BaslangicTarihi,
            BitisTarihi = vm.BitisTarihi,
            KiraBedeli = vm.KiraBedeli,
            Periyot = vm.Periyot,
            Depozito = vm.Depozito,
            Notlar = vm.Notlar,
            Durum = SozlesmeDurumu.Aktif,
            KdvUygulanacakMi = vm.KdvUygulanacakMi,
            KdvOrani = vm.KdvUygulanacakMi ? vm.KdvOrani : 0
        };

        await _sozlesmeService.CreateAsync(s);
        TempData["Success"] = "Sözleşme başarıyla oluşturuldu.";
        return RedirectToAction("Detay", new { id = s.Id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Extend)]
    public async Task<IActionResult> Uzat(int id, SozlesmeUzatViewModel vm)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        if (s.Durum == SozlesmeDurumu.Feshedildi)
        {
            TempData["Error"] = "Feshedilmiş sözleşme uzatılamaz.";
            return RedirectToAction("Detay", new { id });
        }

        if (vm.YeniBitisTarihi <= s.BitisTarihi)
            ModelState.AddModelError("YeniBitisTarihi", "Yeni bitiş tarihi mevcut bitiş tarihinden büyük olmalıdır.");

        if (vm.YeniKiraBedeli <= 0)
            ModelState.AddModelError("YeniKiraBedeli", "Yeni kira bedeli sıfırdan büyük olmalıdır.");

        if (vm.TufeUygulanacakMi && vm.TufeOrani.HasValue && vm.TufeOrani.Value < 0)
            ModelState.AddModelError("TufeOrani", "TÜFE oranı negatif olamaz.");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction("Detay", new { id });
        }

        await _sozlesmeService.UzatAsync(id, vm.YeniBitisTarihi, vm.YeniKiraBedeli,
            vm.KdvUygulanacakMi, vm.KdvOrani ?? 20, vm.TufeOrani, vm.Aciklama);

        TempData["Success"] = "Sözleşme süresi başarıyla uzatıldı.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Terminate)]
    public async Task<IActionResult> Feshet(int id, SozlesmeFesihViewModel vm)
    {
        var s = await _sozlesmeService.GetByIdAsync(id);
        if (s == null) return NotFound();

        if (s.Durum == SozlesmeDurumu.Feshedildi)
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
        TempData["Success"] = "Sözleşme başarıyla feshedildi.";
        return RedirectToAction("Detay", new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Sozlesme.Edit)]
    public IActionResult HesaplaTufeKdv(decimal mevcutBedel, decimal? tufeOrani, bool kdvUygulanacakMi, decimal? kdvOrani)
    {
        var sonuc = _istatistik.HesaplaKiraArtisi(mevcutBedel, tufeOrani, kdvUygulanacakMi, kdvOrani);
        return Json(sonuc);
    }
}

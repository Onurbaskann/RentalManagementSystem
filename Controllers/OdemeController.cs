using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Controllers;

[Authorize]
public class OdemeController : Controller
{
    private readonly IOdemeService _odemeService;
    private readonly ITahakkukService _tahakkukService;
    private readonly IDekontService _dekontService;
    private readonly IBankaHareketiService _bankaService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public OdemeController(
        IOdemeService odemeService,
        ITahakkukService tahakkukService,
        IDekontService dekontService,
        IBankaHareketiService bankaService,
        UserManager<ApplicationUser> userManager,
        IConfiguration config)
    {
        _odemeService = odemeService;
        _tahakkukService = tahakkukService;
        _dekontService = dekontService;
        _bankaService = bankaService;
        _userManager = userManager;
        _config = config;
    }

    [Authorize(Policy = PermissionCatalog.Odeme.View)]
    public async Task<IActionResult> Index(int? tahakkukId, string? durum = null)
    {
        var userId = User.IsInRole("Goruntuleyici") ? _userManager.GetUserId(User) : null;
        var odemeler = await _odemeService.GetAllAsync(tahakkukId, userId);

        odemeler = durum switch {
            "onaybekliyor" => odemeler.Where(o => o.Durum == OdemeDurumu.OnayBekliyor).ToList(),
            "onaylandi"    => odemeler.Where(o => o.Durum == OdemeDurumu.Onaylandi).ToList(),
            "reddedildi"   => odemeler.Where(o => o.Durum == OdemeDurumu.Reddedildi).ToList(),
            _              => odemeler
        };

        ViewBag.TahakkukId = tahakkukId;
        ViewBag.Durum = durum ?? "tum";
        return View(odemeler);
    }

    [Authorize(Policy = PermissionCatalog.Odeme.View)]
    public async Task<IActionResult> Detay(int id)
    {
        var odeme = await _odemeService.GetByIdAsync(id);
        if (odeme == null) return NotFound();

        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User)!;
            var yetkiliTasinmazIds = await GetYetkiliTasinmazIdsAsync(userId);
            if (!yetkiliTasinmazIds.Contains(odeme.KiraTahakkuk.KiraSozlesmesi.Birim.TasinmazId))
                return Forbid();
        }

        return View(odeme);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Odeme.Create)]
    public async Task<IActionResult> Ekle(int tahakkukId)
    {
        var tahakkuk = await _tahakkukService.GetByIdAsync(tahakkukId);
        if (tahakkuk == null) return NotFound();

        var vm = new OdemeEkleViewModel
        {
            KiraTahakkukId = tahakkukId,
            KiraSozlesmesiId = tahakkuk.KiraSozlesmesiId,
            Tutar = tahakkuk.ToplamTutar - tahakkuk.OdenenTutar,
            Tahakkuk = tahakkuk
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Odeme.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(OdemeEkleViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Tahakkuk = await _tahakkukService.GetByIdAsync(vm.KiraTahakkukId);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;
        var odeme = new KiraOdeme
        {
            KiraTahakkukId = vm.KiraTahakkukId,
            KiraSozlesmesiId = vm.KiraSozlesmesiId,
            OdemeTarihi = vm.OdemeTarihi,
            Tutar = vm.Tutar,
            OdemeKanali = vm.OdemeKanali,
            Aciklama = vm.Aciklama,
            GirenUserId = userId
        };

        await _odemeService.EkleAsync(odeme);
        TempData["Success"] = "Ödeme kaydedildi, onay bekleniyor.";
        return RedirectToAction(nameof(Detay), new { id = odeme.Id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Odeme.Approve)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onayla(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var basarili = await _odemeService.OnaylaAsync(id, userId);
        TempData[basarili ? "Success" : "Error"] = basarili ? "Ödeme onaylandı." : "Ödeme onaylanamadı.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Odeme.Reject)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reddet(OdemeRedViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Neden))
        {
            TempData["Error"] = "Red nedeni zorunludur.";
            return RedirectToAction(nameof(Detay), new { id = vm.OdemeId });
        }

        var basarili = await _odemeService.ReddetAsync(vm.OdemeId, vm.Neden);
        TempData[basarili ? "Success" : "Error"] = basarili ? "Ödeme reddedildi." : "Ödeme reddedilemedi.";
        return RedirectToAction(nameof(Detay), new { id = vm.OdemeId });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Odeme.UploadDekont)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DekontYukle(DekontYukleViewModel vm)
    {
        if (vm.Dosya == null || vm.Dosya.Length == 0)
        {
            TempData["Error"] = "Dosya seçiniz.";
            return RedirectToAction(nameof(Detay), new { id = vm.OdemeId });
        }

        var maxMb = _config.GetValue<int>("MaxDekontFileSizeMb", 5);
        if (vm.Dosya.Length > maxMb * 1024 * 1024)
        {
            TempData["Error"] = $"Dosya boyutu {maxMb} MB'ı aşamaz.";
            return RedirectToAction(nameof(Detay), new { id = vm.OdemeId });
        }

        var userId = _userManager.GetUserId(User)!;
        await _dekontService.EkleAsync(vm.OdemeId, vm.Dosya, userId);
        TempData["Success"] = "Dekont yüklendi.";
        return RedirectToAction(nameof(Detay), new { id = vm.OdemeId });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Odeme.UploadDekont)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DekontSil(int id, int odemeId)
    {
        await _dekontService.SilAsync(id);
        TempData["Success"] = "Dekont silindi.";
        return RedirectToAction(nameof(Detay), new { id = odemeId });
    }

    [Authorize(Policy = PermissionCatalog.Odeme.View)]
    public async Task<IActionResult> DekontIndir(int id)
    {
        var dekont = await _dekontService.GetByIdAsync(id);
        if (dekont == null) return NotFound();

        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User)!;
            var yetkiliIds = await GetYetkiliTasinmazIdsAsync(userId);
            var odeme = await _odemeService.GetByIdAsync(dekont.KiraOdemeId);
            if (odeme == null || !yetkiliIds.Contains(odeme.KiraTahakkuk.KiraSozlesmesi.Birim.TasinmazId))
                return Forbid();
        }

        var tamYol = _dekontService.GetTamYol(dekont);
        if (!System.IO.File.Exists(tamYol)) return NotFound();

        return PhysicalFile(tamYol, dekont.DosyaTipi, dekont.OrijinalDosyaAdi);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Odeme.MatchBankTransaction)]
    public async Task<IActionResult> HareketSec(int id)
    {
        var odeme = await _odemeService.GetByIdAsync(id);
        if (odeme == null) return NotFound();

        if (User.IsInRole("Goruntuleyici"))
        {
            var userId = _userManager.GetUserId(User)!;
            var yetkiliIds = await GetYetkiliTasinmazIdsAsync(userId);
            if (!yetkiliIds.Contains(odeme.KiraTahakkuk.KiraSozlesmesi.Birim.TasinmazId))
                return Forbid();
        }

        var adaylar = await _bankaService.GetHareketAdaylariAsync(id);
        return View(new OdemeHareketSecViewModel { Odeme = odeme, HareketAdaylari = adaylar });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Odeme.MatchBankTransaction)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankaEslesmeKaldir(int eslesmeId, int odemeId)
    {
        await _bankaService.EslesmeCozAsync(eslesmeId);
        TempData["Success"] = "Banka eşleşmesi kaldırıldı.";
        return RedirectToAction(nameof(Detay), new { id = odemeId });
    }

    private async Task<List<int>> GetYetkiliTasinmazIdsAsync(string userId)
    {
        var userTasinmazIds = await _odemeService.GetAllAsync(userId: userId);
        return userTasinmazIds
            .Select(o => o.KiraTahakkuk?.KiraSozlesmesi?.Birim?.TasinmazId ?? 0)
            .Where(id => id > 0).Distinct().ToList();
    }
}

using KiraTakip.Authorization;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
public class OdemeController : Controller
{
    private readonly IOdemeService _odemeService;
    private readonly ITahakkukService _tahakkukService;
    private readonly IBelgeService _belgeService;
    private readonly IBankaHareketiService _bankaService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IYetkiKapsamiProvider _provider;

    public OdemeController(
        IOdemeService odemeService,
        ITahakkukService tahakkukService,
        IBelgeService belgeService,
        IBankaHareketiService bankaService,
        UserManager<ApplicationUser> userManager,
        IYetkiKapsamiProvider provider)
    {
        _odemeService = odemeService;
        _tahakkukService = tahakkukService;
        _belgeService = belgeService;
        _bankaService = bankaService;
        _userManager = userManager;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Odeme.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query, int? tahakkukId = null)
    {
        var paged = await _odemeService.GetPagedAsync(query, tahakkukId, _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds);

        ViewBag.TahakkukId = tahakkukId;
        ViewBag.Query = query;
        ViewBag.Durum = query.Durum ?? "tum";
        return View(paged);
    }

    [Authorize(Policy = PermissionCatalog.Odeme.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        var odeme = await _odemeService.GetByIdAsync(id);
        if (odeme == null) return NotFound();

        if (odeme.TasinmazId != null && !_provider.KapsamdaMi(odeme.TasinmazId.Value))
            return Forbid();

        ViewBag.Belgeler    = await _belgeService.GetListAsync(BelgeOwnerTipi.Odeme, id);
        ViewBag.BelgeTurleri = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Odeme);
        return View(odeme);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Odeme.Create)]
    public async Task<IActionResult> Ekle(int tahakkukId)
    {
        var tahakkuk = await _tahakkukService.GetDetayAsync(tahakkukId);
        if (tahakkuk == null) return NotFound();

        var vm = new OdemeEkleViewModel
        {
            TahakkukId = tahakkukId,
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
            vm.Tahakkuk = await _tahakkukService.GetDetayAsync(vm.TahakkukId);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;
        var odeme = new TahakkukOdeme
        {
            TahakkukId = vm.TahakkukId,
            KiraSozlesmesiId = vm.KiraSozlesmesiId,
            OdemeTarihi = vm.OdemeTarihi,
            Tutar = vm.Tutar,
            OdemeKanali = vm.OdemeKanali,
            OdemeKaynakTipi = OdemeKaynakTipi.Manuel,
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


    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Odeme.MatchBankTransaction)]
    public async Task<IActionResult> HareketSec(int id)
    {
        var odeme = await _odemeService.GetByIdAsync(id);
        if (odeme == null) return NotFound();

        if (odeme.TasinmazId != null && !_provider.KapsamdaMi(odeme.TasinmazId.Value))
            return Forbid();

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

}

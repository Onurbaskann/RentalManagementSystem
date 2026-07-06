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
    private readonly IPaymentService _paymentService;
    private readonly IChargeService _chargeService;
    private readonly IDocumentService _belgeService;
    private readonly IBankTransactionService _bankaService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionScopeProvider _provider;

    public OdemeController(
        IPaymentService odemeService,
        IChargeService tahakkukService,
        IDocumentService documentService,
        IBankTransactionService bankaService,
        UserManager<ApplicationUser> userManager,
        IPermissionScopeProvider provider)
    {
        _paymentService = odemeService;
        _chargeService = tahakkukService;
        _belgeService = documentService;
        _bankaService = bankaService;
        _userManager = userManager;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query, int? tahakkukId = null)
    {
        var paged = await _paymentService.GetPagedAsync(query, tahakkukId, _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds);

        ViewBag.ChargeId = tahakkukId;
        ViewBag.Query = query;
        ViewBag.Durum = query.Durum ?? "tum";
        return View(paged);
    }

    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Detay(int id)
    {
        var odeme = await _paymentService.GetByIdAsync(id);
        if (odeme == null) return NotFound();

        if (odeme.TasinmazId != null && !_provider.KapsamdaMi(odeme.TasinmazId.Value))
            return Forbid();

        ViewBag.Belgeler    = await _belgeService.GetListAsync(BelgeOwnerTipi.Odeme, id);
        ViewBag.DocumentTypes = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Odeme);
        return View(odeme);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Payment.Create)]
    public async Task<IActionResult> Ekle(int tahakkukId)
    {
        var tahakkuk = await _chargeService.GetDetayAsync(tahakkukId);
        if (tahakkuk == null) return NotFound();

        var vm = new OdemeEkleViewModel
        {
            ChargeId = tahakkukId,
            LeaseId = tahakkuk.LeaseId,
            Amount = tahakkuk.ToplamTutar - tahakkuk.PaidAmount,
            Charge = tahakkuk
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ekle(OdemeEkleViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Charge = await _chargeService.GetDetayAsync(vm.ChargeId);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;
        var odeme = new PaymentAllocation
        {
            ChargeId = vm.ChargeId,
            LeaseId = vm.LeaseId,
            PaymentDate = vm.PaymentDate,
            Amount = vm.Amount,
            PaymentChannel = vm.PaymentChannel,
            PaymentSourceType = PaymentSourceType.Manual,
            Description = vm.Aciklama,
            CreatedByUserId = userId
        };

        await _paymentService.EkleAsync(odeme);
        TempData["Success"] = "Ödeme kaydedildi, onay bekleniyor.";
        return RedirectToAction(nameof(Detay), new { id = odeme.Id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.Approve)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Onayla(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var basarili = await _paymentService.OnaylaAsync(id, userId);
        TempData[basarili ? "Success" : "Error"] = basarili ? "Ödeme onaylandı." : "Ödeme onaylanamadı.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.Reject)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reddet(OdemeRedViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Neden))
        {
            TempData["Error"] = "Red nedeni zorunludur.";
            return RedirectToAction(nameof(Detay), new { id = vm.OdemeId });
        }

        var basarili = await _paymentService.ReddetAsync(vm.OdemeId, vm.Neden);
        TempData[basarili ? "Success" : "Error"] = basarili ? "Ödeme reddedildi." : "Ödeme reddedilemedi.";
        return RedirectToAction(nameof(Detay), new { id = vm.OdemeId });
    }


    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    public async Task<IActionResult> HareketSec(int id)
    {
        var odeme = await _paymentService.GetByIdAsync(id);
        if (odeme == null) return NotFound();

        if (odeme.TasinmazId != null && !_provider.KapsamdaMi(odeme.TasinmazId.Value))
            return Forbid();

        var adaylar = await _bankaService.GetHareketAdaylariAsync(id);
        return View(new OdemeHareketSecViewModel { Odeme = odeme, HareketAdaylari = adaylar });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BankaEslesmeKaldir(int eslesmeId, int odemeId)
    {
        await _bankaService.EslesmeCozAsync(eslesmeId);
        TempData["Success"] = "Banka eşleşmesi kaldırıldı.";
        return RedirectToAction(nameof(Detay), new { id = odemeId });
    }

}

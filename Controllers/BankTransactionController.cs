using KiraTakip.Authorization;
using KiraTakip.Models.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
public class BankTransactionController : Controller
{
    private readonly IBankTransactionService _bankaService;
    private readonly IPaymentService _paymentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionScopeProvider _provider;

    public BankTransactionController(
        IBankTransactionService bankaService,
        IPaymentService odemeService,
        UserManager<ApplicationUser> userManager,
        IPermissionScopeProvider provider)
    {
        _bankaService = bankaService;
        _paymentService = odemeService;
        _userManager = userManager;
        _provider = provider;
    }

    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var paged = await _bankaService.GetPagedAsync(query);
        ViewBag.Query = query;
        ViewBag.Durum = query.Durum ?? "tum";
        return View(paged);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Payment.ImportBankStatement)]
    public IActionResult Import()
    {
        return View(new BankaImportViewModel());
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.ImportBankStatement)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(BankaImportViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (vm.Dosya == null || vm.Dosya.Length == 0)
        {
            ModelState.AddModelError("Dosya", "CSV dosyası seçiniz.");
            return View(vm);
        }

        try
        {
            await using var stream = vm.Dosya.OpenReadStream();
            var adet = await _bankaService.ImportAsync(stream, vm.BankCode);
            TempData["Success"] = $"{adet} hareket içe aktarıldı.";
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    public async Task<IActionResult> EslestirSec(int id)
    {
        var hareketi = await _bankaService.GetByIdAsync(id);
        if (hareketi == null) return NotFound();

        var tasinmazIds = _provider.GlobalErisim ? null : _provider.ErisilebilirTasinmazIds;
        var adaylar = await _bankaService.GetOdemeAdaylariAsync(id, tasinmazIds);

        return View(new BankaEslesmeSecViewModel { BankTransaction = hareketi, OdemeAdaylari = adaylar });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eslestir(EslesmeViewModel vm)
    {
        await _bankaService.EslestirAsync(vm.OdemeId, vm.BankTransactionId);
        TempData["Success"] = "Eşleştirme yapıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EslesmeCoz(int eslesmeId)
    {
        await _bankaService.EslesmeCozAsync(eslesmeId);
        TempData["Success"] = "Eşleştirme çözüldü.";
        return RedirectToAction(nameof(Index));
    }
}

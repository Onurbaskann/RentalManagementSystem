using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[Authorize(Policy = PermissionCatalog.TenantPortal.Charge.Module)]
[Route("Tenant/Tahakkuklarim")]
public class TenantChargeController : Controller
{
    private readonly IChargeService _chargeService;
    private readonly IPaymentService _paymentService;
    private readonly IDocumentService _belgeService;
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;

    public TenantChargeController(
        IChargeService tahakkukService,
        IPaymentService odemeService,
        IDocumentService documentService,
        ApplicationDbContext ctx,
        UserManager<ApplicationUser> userManager)
    {
        _chargeService = tahakkukService;
        _paymentService = odemeService;
        _belgeService = documentService;
        _ctx = ctx;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        await _chargeService.GecikmeleriGuncelleAsync();

        // Query filter (DbContext) kiracı bazlı filtrelemeyi otomatik uygular.
        var paged = await _chargeService.GetPagedAsync(query);

        // Özet kartlar
        var toplamTahakkuk = await _ctx.Charges
            .Where(t => t.Status != ChargeStatus.Cancelled)
            .SumAsync(t => (decimal?)t.TotalAmount) ?? 0m;

        var tahsilEdilen = await _ctx.PaymentAllocations
            .Where(o => o.Status == PaymentStatus.Approved)
            .SumAsync(o => (decimal?)o.Amount) ?? 0m;

        var kalanBorc = await _ctx.Charges
            .Where(t => t.Status == ChargeStatus.Pending
                     || t.Status == ChargeStatus.PartiallyPaid
                     || t.Status == ChargeStatus.Overdue)
            .SumAsync(t => (decimal?)(t.TotalAmount - t.PaidAmount)) ?? 0m;

        var gecikmisKalan = await _ctx.Charges
            .Where(t => t.Status == ChargeStatus.Overdue)
            .SumAsync(t => (decimal?)(t.TotalAmount - t.PaidAmount)) ?? 0m;

        ViewBag.ToplamTahakkuk = toplamTahakkuk;
        ViewBag.TahsilEdilen = tahsilEdilen;
        ViewBag.KalanBorc = kalanBorc;
        ViewBag.GecikmisKalan = gecikmisKalan;

        ViewBag.Units = await _ctx.Units
            .Where(b => _ctx.Leases.Any(s => s.UnitId == b.Id))
            .OrderBy(b => b.Name)
            .ToListAsync();
        ViewBag.MevcutYillar = await _ctx.Charges
            .Select(t => t.PeriodStart.Year).Distinct()
            .OrderByDescending(y => y).ToListAsync();

        ViewBag.Query = query;
        ViewBag.Durum = query.Durum ?? "tum";
        return View(paged);
    }

    [HttpGet("Detay/{id:int}")]
    public async Task<IActionResult> Detay(int id)
    {
        var charge = await _chargeService.GetDetayAsync(id);
        if (charge == null) return NotFound();

        var belgeTurleri = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Payment);
        var odemeIdleri = charge.Allocations.Select(o => o.Id).ToList();
        var tumBelgeler = new Dictionary<int, List<Belge>>();
        foreach (var oid in odemeIdleri)
            tumBelgeler[oid] = await _belgeService.GetListAsync(BelgeOwnerTipi.Payment, oid);

        ViewBag.DocumentTypes = belgeTurleri;
        ViewBag.OdemeBelgeleri = tumBelgeler;

        return View(charge);
    }

    [HttpPost("OdemeBildir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OdemeBildir(
        int tahakkukId,
        decimal tutar,
        DateTime odemeTarihi,
        PaymentChannel odemeKanali,
        string? aciklama,
        IFormFile? dekont)
    {
        var charge = await _ctx.Charges.FirstOrDefaultAsync(t => t.Id == tahakkukId);
        if (charge == null) return NotFound();

        var kalan = charge.TotalAmount - charge.PaidAmount;
        if (tutar <= 0 || tutar > kalan)
        {
            TempData["Error"] = $"Amount 0'dan büyük ve kalan borçtan ({kalan:N2} ₺) küçük/eşit olmalıdır.";
            return RedirectToAction(nameof(Detay), new { id = tahakkukId });
        }
        if (dekont == null || dekont.Length == 0)
        {
            TempData["Error"] = "Dekont yüklemeniz zorunludur.";
            return RedirectToAction(nameof(Detay), new { id = tahakkukId });
        }

        var userId = _userManager.GetUserId(User)!;
        var payment = new PaymentAllocation
        {
            ChargeId = tahakkukId,
            LeaseId = charge.LeaseId,
            PaymentDate = odemeTarihi,
            Amount = tutar,
            PaymentChannel = odemeKanali,
            PaymentSourceType = PaymentSourceType.Manual,
            Description = aciklama,
            CreatedByUserId = userId
        };
        await _paymentService.EkleAsync(payment);

        // Dekont yükle
        var turleri = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Payment);
        if (turleri.Any())
        {
            var belgeTuru = turleri.First();
            if (dekont.Length <= belgeTuru.MaxSizeMb * 1024 * 1024)
            {
                using var ms = new MemoryStream();
                await dekont.CopyToAsync(ms);
                await _belgeService.UploadAsync(BelgeOwnerTipi.Payment, payment.Id, belgeTuru.Id,
                    dekont.FileName, dekont.ContentType, ms.ToArray(), invalidateOld: false);
            }
        }

        TempData["Success"] = "Ödeme bildiriminiz alındı. Yönetici onayı sonrası tahakkuka işlenecektir.";
        return RedirectToAction(nameof(Detay), new { id = tahakkukId });
    }
}

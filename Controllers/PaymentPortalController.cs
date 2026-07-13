using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Settings;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KiraTakip.Controllers;

[AllowAnonymous]
public class PaymentPortalController : Controller
{
    private readonly IPaymentLinkService _paymentLink;
    private readonly ApplicationDbContext _ctx;
    private readonly PaymentLinkSettings _paymentLinkSettings;

    public PaymentPortalController(
        IPaymentLinkService paymentLink,
        ApplicationDbContext ctx,
        IOptions<PaymentLinkSettings> paymentLinkOptions)
    {
        _paymentLink = paymentLink;
        _ctx = ctx;
        _paymentLinkSettings = paymentLinkOptions.Value;
    }

    [Route("Payment/Portal")]
    public async Task<IActionResult> Index(string t)
    {
        var validation = await _paymentLink.TryValidateAsync(t);
        if (!validation.Success)
            return View("Invalid", validation.Reason ?? "Geçersiz veya süresi dolmuş ödeme linki.");
        var tenantId = validation.TenantId;

        var tenant = await _ctx.Tenants.FirstOrDefaultAsync(k => k.Id == tenantId);
        if (tenant == null) return View("Invalid", "Kiracı bulunamadı.");

        var vadeEsigi = DateTime.Today.AddDays(_paymentLinkSettings.ReminderDaysBefore);

        var borclar = await _ctx.Charges
            .Include(x => x.Lease!).ThenInclude(s => s!.Unit).ThenInclude(b => b.Property)
            .Include(x => x.Allocations)
            .Where(x => x.Lease!.TenantId == tenantId
                     && x.Status != ChargeStatus.Paid
                     && x.Status != ChargeStatus.Cancelled
                     && x.DueDate <= vadeEsigi)
            .OrderBy(x => x.DueDate)
            .ToListAsync();

        if (borclar.Count == 0)
        {
            var noDebtModel = new KiraciOdemePortalViewModel
            {
                KiraciId = tenant.Id,
                Ad = tenant.Name,
                Soyad = "",
                Email = tenant.Email ?? ""
            };
            return View("NoDebt", noDebtModel);
        }

        var vm = new KiraciOdemePortalViewModel
        {
            KiraciId = tenant.Id,
            Ad = tenant.Name,
            Soyad = "",
            Email = tenant.Email ?? "",
            Borclar = borclar.Select(b => new BorcKart
            {
                ChargeId = b.Id,
                PropertyName = b.Lease!.Unit!.Property!.Name,
                BirimAdi = b.Lease!.Unit!.Name,
                PeriodStart = b.PeriodStart,
                DueDate = b.DueDate,
                ToplamTutar = b.TotalAmount,
                PaidAmount = b.Allocations.Where(o => o.Status == PaymentStatus.Approved).Sum(o => o.Amount)
            }).ToList(),
            DefaultSelectedId = borclar.First().Id
        };

        return View(vm);
    }
}

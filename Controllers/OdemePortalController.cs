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
public class OdemePortalController : Controller
{
    private readonly IPaymentLinkService _paymentLink;
    private readonly ApplicationDbContext _ctx;
    private readonly PaymentLinkSettings _paymentLinkSettings;

    public OdemePortalController(
        IPaymentLinkService paymentLink,
        ApplicationDbContext ctx,
        IOptions<PaymentLinkSettings> paymentLinkOptions)
    {
        _paymentLink = paymentLink;
        _ctx = ctx;
        _paymentLinkSettings = paymentLinkOptions.Value;
    }

    [Route("Odeme/Portal")]
    public async Task<IActionResult> Index(string t)
    {
        if (!_paymentLink.TryValidate(t, out var kiraciId, out var reason))
            return View("Invalid", reason ?? "Geçersiz veya süresi dolmuş ödeme linki.");

        var kiraci = await _ctx.Kiraciler.FirstOrDefaultAsync(k => k.Id == kiraciId);
        if (kiraci == null) return View("Invalid", "Kiracı bulunamadı.");

        var vadeEsigi = DateTime.Today.AddDays(_paymentLinkSettings.ReminderDaysBefore);

        var borclar = await _ctx.KiraTahakkuklar
            .Include(x => x.KiraSozlesmesi!).ThenInclude(s => s!.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(x => x.Odemeler)
            .Where(x => x.KiraSozlesmesi!.KiraciId == kiraciId
                     && x.Durum != TahakkukDurumu.TamOdendi
                     && x.Durum != TahakkukDurumu.IptalEdildi
                     && x.VadeTarihi <= vadeEsigi)
            .OrderBy(x => x.VadeTarihi)
            .ToListAsync();

        if (borclar.Count == 0)
        {
            var noDebtModel = new KiraciOdemePortalViewModel
            {
                KiraciId = kiraci.Id,
                Ad = kiraci.Ad,
                Soyad = kiraci.Soyad ?? "",
                Email = kiraci.Email ?? ""
            };
            return View("NoDebt", noDebtModel);
        }

        var vm = new KiraciOdemePortalViewModel
        {
            KiraciId = kiraci.Id,
            Ad = kiraci.Ad,
            Soyad = kiraci.Soyad ?? "",
            Email = kiraci.Email ?? "",
            Borclar = borclar.Select(b => new BorcKart
            {
                TahakkukId = b.Id,
                TasinmazAdi = b.KiraSozlesmesi!.Birim!.Tasinmaz!.Ad,
                BirimAdi = b.KiraSozlesmesi!.Birim!.Ad,
                DonemBaslangic = b.DonemBaslangic,
                VadeTarihi = b.VadeTarihi,
                ToplamTutar = b.ToplamTutar,
                OdenenTutar = b.Odemeler.Where(o => o.Durum == OdemeDurumu.Onaylandi).Sum(o => o.Tutar)
            }).ToList(),
            DefaultSelectedId = borclar.First().Id
        };

        return View(vm);
    }
}

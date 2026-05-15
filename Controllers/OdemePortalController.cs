using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[AllowAnonymous]
public class OdemePortalController : Controller
{
    private readonly IPaymentLinkService _paymentLink;
    private readonly ApplicationDbContext _ctx;

    public OdemePortalController(IPaymentLinkService paymentLink, ApplicationDbContext ctx)
    {
        _paymentLink = paymentLink;
        _ctx = ctx;
    }

    [Route("Odeme/Portal/{id:int}")]
    public async Task<IActionResult> Index(int id, string t)
    {
        if (!_paymentLink.TryValidate(id, t, out var reason))
            return View("Invalid", reason ?? "Geçersiz veya süresi dolmuş ödeme linki.");

        var tahakkuk = await _ctx.KiraTahakkuklar
            .Include(x => x.KiraSozlesmesi!).ThenInclude(s => s!.Kiraci)
            .Include(x => x.KiraSozlesmesi!).ThenInclude(s => s!.Birim).ThenInclude(b => b.Tasinmaz)
            .Include(x => x.Odemeler)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tahakkuk == null) return NotFound();

        var odenmis = tahakkuk.Odemeler
            .Where(o => o.Durum == OdemeDurumu.Onaylandi)
            .Sum(o => o.Tutar);

        var vm = new OdemePortalViewModel
        {
            TahakkukId = tahakkuk.Id,
            KiraciAdi = tahakkuk.KiraSozlesmesi?.Kiraci?.GosterimAdi ?? "",
            TasinmazAdi = tahakkuk.KiraSozlesmesi?.Birim?.Tasinmaz?.Ad ?? "",
            BirimAdi = tahakkuk.KiraSozlesmesi?.Birim?.Ad ?? "",
            DonemBaslangic = tahakkuk.DonemBaslangic,
            VadeTarihi = tahakkuk.VadeTarihi,
            ToplamTutar = tahakkuk.ToplamTutar,
            OdenenTutar = odenmis
        };

        return View(vm);
    }
}

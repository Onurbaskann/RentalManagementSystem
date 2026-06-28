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

[Authorize(Policy = "KiraciKullanici")]
[Authorize(Policy = PermissionCatalog.KiraciPortal.Borc.View)]
[Route("Kiraci/Tahakkuklarim")]
public class KiraciTahakkukController : Controller
{
    private readonly ITahakkukService _tahakkukService;
    private readonly IOdemeService _odemeService;
    private readonly IBelgeService _belgeService;
    private readonly ApplicationDbContext _ctx;
    private readonly UserManager<ApplicationUser> _userManager;

    public KiraciTahakkukController(
        ITahakkukService tahakkukService,
        IOdemeService odemeService,
        IBelgeService belgeService,
        ApplicationDbContext ctx,
        UserManager<ApplicationUser> userManager)
    {
        _tahakkukService = tahakkukService;
        _odemeService = odemeService;
        _belgeService = belgeService;
        _ctx = ctx;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        await _tahakkukService.GecikmeleriGuncelleAsync();

        // Query filter (DbContext) kiracı bazlı filtrelemeyi otomatik uygular.
        var paged = await _tahakkukService.GetPagedAsync(query);

        // Mutabakat özeti
        var toplamBorc = await _ctx.Tahakkuklar
            .Where(t => t.Durum == TahakkukDurumu.Bekleniyor
                     || t.Durum == TahakkukDurumu.KismenOdendi
                     || t.Durum == TahakkukDurumu.Gecikti)
            .SumAsync(t => (decimal?)(t.ToplamTutar - t.OdenenTutar)) ?? 0m;

        var toplamOdeme = await _ctx.TahakkukOdemeler
            .Where(o => o.Durum == OdemeDurumu.Onaylandi)
            .SumAsync(o => (decimal?)o.Tutar) ?? 0m;

        ViewBag.ToplamBorc = toplamBorc;
        ViewBag.ToplamOdeme = toplamOdeme;
        ViewBag.Bakiye = toplamBorc;

        ViewBag.Birimler = await _ctx.Birimler
            .Where(b => _ctx.Sozlesmeler.Any(s => s.BirimId == b.Id))
            .OrderBy(b => b.Ad)
            .ToListAsync();
        ViewBag.MevcutYillar = await _ctx.Tahakkuklar
            .Select(t => t.DonemBaslangic.Year).Distinct()
            .OrderByDescending(y => y).ToListAsync();

        ViewBag.Query = query;
        ViewBag.Durum = query.Durum ?? "tum";
        return View(paged);
    }

    [HttpGet("Detay/{id:int}")]
    public async Task<IActionResult> Detay(int id)
    {
        var tahakkuk = await _tahakkukService.GetDetayAsync(id);
        if (tahakkuk == null) return NotFound();
        return View(tahakkuk);
    }

    [HttpPost("OdemeBildir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OdemeBildir(
        int tahakkukId,
        decimal tutar,
        DateTime odemeTarihi,
        OdemeKanali odemeKanali,
        string? aciklama,
        IFormFile? dekont)
    {
        var tahakkuk = await _ctx.Tahakkuklar.FirstOrDefaultAsync(t => t.Id == tahakkukId);
        if (tahakkuk == null) return NotFound();

        var kalan = tahakkuk.ToplamTutar - tahakkuk.OdenenTutar;
        if (tutar <= 0 || tutar > kalan)
        {
            TempData["Error"] = $"Tutar 0'dan büyük ve kalan borçtan ({kalan:N2} ₺) küçük/eşit olmalıdır.";
            return RedirectToAction(nameof(Detay), new { id = tahakkukId });
        }
        if (dekont == null || dekont.Length == 0)
        {
            TempData["Error"] = "Dekont yüklemeniz zorunludur.";
            return RedirectToAction(nameof(Detay), new { id = tahakkukId });
        }

        var userId = _userManager.GetUserId(User)!;
        var odeme = new TahakkukOdeme
        {
            TahakkukId = tahakkukId,
            KiraSozlesmesiId = tahakkuk.KiraSozlesmesiId,
            OdemeTarihi = odemeTarihi,
            Tutar = tutar,
            OdemeKanali = odemeKanali,
            OdemeKaynakTipi = OdemeKaynakTipi.Manuel,
            Aciklama = aciklama,
            GirenUserId = userId
        };
        await _odemeService.EkleAsync(odeme);

        // Dekont yükle
        var turleri = await _belgeService.GetTurlerAsync(BelgeOwnerTipi.Odeme);
        if (turleri.Any())
        {
            var belgeTuru = turleri.First();
            if (dekont.Length <= belgeTuru.MaxBoyutMb * 1024 * 1024)
            {
                using var ms = new MemoryStream();
                await dekont.CopyToAsync(ms);
                await _belgeService.UploadAsync(BelgeOwnerTipi.Odeme, odeme.Id, belgeTuru.Id,
                    dekont.FileName, dekont.ContentType, ms.ToArray(), invalidateOld: false);
            }
        }

        TempData["Success"] = "Ödeme bildiriminiz alındı. Yönetici onayı sonrası tahakkuka işlenecektir.";
        return RedirectToAction(nameof(Detay), new { id = tahakkukId });
    }
}

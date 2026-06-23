using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[Authorize(Policy = PermissionCatalog.KiraciPortal.Mutabakat.Manage)]
[Route("Kiraci/Mutabakat")]
public class KiraciMutabakatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public KiraciMutabakatController(ApplicationDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var toplamBorc = await _db.Tahakkuklar
            .Where(t => t.Durum == TahakkukDurumu.Bekleniyor || t.Durum == TahakkukDurumu.KismenOdendi)
            .SumAsync(t => (decimal?)t.ToplamTutar) ?? 0m;

        var toplamOdeme = await _db.KiraOdemeler
            .Where(o => o.Durum == OdemeDurumu.Onaylandi)
            .SumAsync(o => (decimal?)o.Tutar) ?? 0m;

        ViewBag.ToplamBorc = toplamBorc;
        ViewBag.ToplamOdeme = toplamOdeme;
        ViewBag.Bakiye = toplamBorc - toplamOdeme;

        var sonBorclar = await _db.Tahakkuklar
            .Include(t => t.KiraSozlesmesi!).ThenInclude(s => s.Birim)
            .Where(t => t.Durum != TahakkukDurumu.TamOdendi && t.Durum != TahakkukDurumu.IptalEdildi)
            .OrderBy(t => t.VadeTarihi)
            .Take(10)
            .ToListAsync();

        return View(sonBorclar);
    }
}

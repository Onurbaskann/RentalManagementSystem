using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[RequireKiraciId]
[Route("Kiraci/Panel")]
public class KiraciPanelController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public KiraciPanelController(ApplicationDbContext db, ICurrentUserContext currentUser, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var kiraciId = _currentUser.KiraciId!.Value;

        var kiraci = await _db.Kiraciler.FirstOrDefaultAsync(k => k.Id == kiraciId);
        if (kiraci == null) return NotFound();

        var aktifSozlesme = await _db.Sozlesmeler
            .CountAsync(s => s.KiraciId == kiraciId && s.Durum == SozlesmeDurumu.Aktif);

        var bekleyenBorc = await _db.KiraTahakkuklar
            .Where(t => t.KiraSozlesmesiId != null &&
                        t.KiraSozlesmesi!.KiraciId == kiraciId &&
                        t.Durum == TahakkukDurumu.Bekleniyor)
            .SumAsync(t => (decimal?)t.ToplamTutar) ?? 0m;

        ViewBag.KiraciAd = kiraci.GosterimAdi;
        ViewBag.AktifSozlesme = aktifSozlesme;
        ViewBag.BekleyenBorc = bekleyenBorc;

        return View();
    }
}

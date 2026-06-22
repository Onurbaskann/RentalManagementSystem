using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[Authorize(Policy = PermissionCatalog.KiraciPortal.Odeme.View)]
[Route("Kiraci/Odemeler")]
public class KiraciOdemeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public KiraciOdemeController(ApplicationDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var odemeler = await _db.KiraOdemeler
            .Include(o => o.KiraSozlesmesi!)
                .ThenInclude(s => s.Birim)
            .OrderByDescending(o => o.OdemeTarihi)
            .ToListAsync();

        return View(odemeler);
    }
}

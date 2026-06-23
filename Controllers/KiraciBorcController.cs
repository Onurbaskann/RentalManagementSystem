using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[Authorize(Policy = PermissionCatalog.KiraciPortal.Borc.View)]
[Route("Kiraci/Borclar")]
public class KiraciBorcController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public KiraciBorcController(ApplicationDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tahakkuklar = await _db.Tahakkuklar
            .Include(t => t.KiraSozlesmesi!)
                .ThenInclude(s => s.Birim)
            .OrderByDescending(t => t.VadeTarihi)
            .ToListAsync();

        return View(tahakkuklar);
    }
}

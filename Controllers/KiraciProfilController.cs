using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[RequireKiraciId]
[Route("Kiraci/Profil")]
public class KiraciProfilController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public KiraciProfilController(ApplicationDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var kiraciId = _currentUser.KiraciId!.Value;
        var kiraci = await _db.Kiraciler
            .Include(k => k.KiraciKategori)
            .Include(k => k.Sektor)
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == kiraciId);

        if (kiraci == null) return NotFound();

        return View(kiraci);
    }
}

using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Route("Tenant/Profil")]
public class TenantProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public TenantProfileController(ApplicationDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tenantId = _currentUser.KiraciId!.Value;
        var tenant = await _db.Tenants
            .Include(k => k.TenantCategory)
            .Include(k => k.Sector)
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == tenantId);

        if (tenant == null) return NotFound();

        return View(tenant);
    }
}

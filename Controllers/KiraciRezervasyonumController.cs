using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "KiraciKullanici")]
[Authorize(Policy = PermissionCatalog.KiraciPortal.Rezervasyon.View)]
[Route("Kiraci/Rezervasyonum")]
public class KiraciRezervasyonumController : Controller
{
    private readonly IRezervasyonService _rezervasyonService;
    private readonly ICurrentUserContext _currentUser;

    public KiraciRezervasyonumController(IRezervasyonService rezervasyonService, ICurrentUserContext currentUser)
    {
        _rezervasyonService = rezervasyonService;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _rezervasyonService.GetAllAsync();
        return View(list);
    }
}

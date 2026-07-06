using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[Authorize(Policy = PermissionCatalog.TenantPortal.Reservation.Module)]
[Route("Tenant/Rezervasyonum")]
public class TenantReservationController : Controller
{
    private readonly IReservationService _reservationService;
    private readonly ICurrentUserContext _currentUser;

    public TenantReservationController(IReservationService rezervasyonService, ICurrentUserContext currentUser)
    {
        _reservationService = rezervasyonService;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var list = await _reservationService.GetAllAsync();
        return View(list);
    }
}

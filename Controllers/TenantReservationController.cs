using KiraTakip.Authorization;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[Authorize(Policy = PermissionCatalog.TenantPortal.Reservation.Module)]
[RequireKiraciId]
[Route("Tenant/MyReservations")]
public class TenantReservationController(
    IReservationService reservationService,
    ICurrentUserContext currentUserContext,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tenantId = currentUserContext.TenantId!.Value;
        var list = await reservationService.GetTenantReservationsAsync(
            new GetTenantReservationsInput(
                tenantId,
                DateTime.Now,
                permissionScopeProvider.GlobalAccess
                    ? new ReservationAccessScopeInput()
                    : new ReservationAccessScopeInput(
                        permissionScopeProvider.AccessiblePropertyIds,
                        permissionScopeProvider.AccessibleUnitIds)));
        return View(list);
    }
}

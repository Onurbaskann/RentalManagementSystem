using KiraTakip.Authorization;
using KiraTakip.Models.Dtos;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models.Dtos.Charge;

namespace KiraTakip.Web.Controllers;

[Authorize]
[Route("Charge")]
public class ChargeController(
    IChargeService chargeService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query, int? spaceId)
    {
        await chargeService.UpdateDelaysAsync();

        var scope = permissionScopeProvider.SpaceScope;

        var pagedResult = await chargeService.GetPagedAsync(
            new GetChargesPageInput(query, Scope: scope, SpaceId: spaceId));
        var options = await chargeService.GetIndexOptionsAsync(
            new GetChargeIndexOptionsInput(scope, query.Status));

        var model = new ChargeIndexViewModel
        {
            Charges = pagedResult,
            Query = query,
            SpaceId = spaceId,
            Status = query.Status ?? "tum",
            CancelledCount = options.CancelledCount,
            Spaces = options.Spaces,
            Tenants = options.Tenants,
            AvailableYears = options.AvailableYears,
        };

        return View(model);
    }

    [HttpGet("Details/{id}")]
    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Details(int id)
    {
        var charge = await chargeService.GetDetailsAsync(new GetChargeDetailsInput(id));
        if (charge == null) return NotFound();

        if (!IsInScope(charge))
            return Forbid();

        return View(charge);
    }

    private bool IsInScope(ChargeDetailDto charge)
        => permissionScopeProvider.GlobalAccess
            || (!string.IsNullOrEmpty(charge.SpacePath)
                && permissionScopeProvider.IsSpaceInScope(charge.SpacePath));
}

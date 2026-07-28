using KiraTakip.Authorization;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Charge")]
public class ChargeController(
    IChargeService chargeService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        await chargeService.UpdateDelaysAsync();

        var propertyIds = permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessiblePropertyIds;
        var unitIds = (!permissionScopeProvider.GlobalAccess && permissionScopeProvider.AccessibleUnitIds.Count > 0)
            ? permissionScopeProvider.AccessibleUnitIds
            : null;

        var pagedResult = await chargeService.GetPagedAsync(
            new GetChargesPageInput(query, PropertyIds: propertyIds, UnitIds: unitIds));
        var options = await chargeService.GetIndexOptionsAsync(
            new GetChargeIndexOptionsInput(
                permissionScopeProvider.GlobalAccess,
                propertyIds,
                unitIds,
                query.Status));

        var model = new ChargeIndexViewModel
        {
            Charges = pagedResult,
            Query = query,
            Status = query.Status ?? "tum",
            CancelledCount = options.CancelledCount,
            Properties = options.Properties,
            Units = options.Units,
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
            || (charge.PropertyId.HasValue
                && permissionScopeProvider.AccessiblePropertyIds.Contains(charge.PropertyId.Value))
            || (charge.UnitId.HasValue
                && permissionScopeProvider.AccessibleUnitIds.Contains(charge.UnitId.Value));
}

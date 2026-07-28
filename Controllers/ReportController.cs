using KiraTakip.Authorization;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.Payment.Module)]
[Route("Report")]
public class ReportController(
    IChargeService chargeService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(ReportQueryViewModel query)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var selectedYear = query.Year ?? DateTime.Today.Year;
        var propertyIds = permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessiblePropertyIds;
        var unitIds = permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessibleUnitIds;

        var report = await chargeService.GetMonthlyCollectionReportAsync(
            new GetMonthlyCollectionReportInput(
                selectedYear,
                DateTime.Today,
                propertyIds,
                unitIds));

        return View(report.ToViewModel());
    }
}

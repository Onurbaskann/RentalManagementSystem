using KiraTakip.Authorization;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models.Dtos.Report;

namespace KiraTakip.Web.Controllers;

[Authorize(Policy = PermissionCatalog.Payment.Module)]
[Route("Report")]
public class ReportController(
    IReportService reportService,
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

        var report = await reportService.GetMonthlyCollectionReportAsync(
            new GetMonthlyCollectionReportInput(
                selectedYear,
                DateTime.Today,
                query.StoreId,
                query.ChargeTypeId,
                propertyIds,
                unitIds));

        return View(report.ToViewModel());
    }
}

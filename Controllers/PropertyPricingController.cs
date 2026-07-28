using KiraTakip.Authorization;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Property/{propertyId}/Pricing")]
public class PropertyPricingController(
    IPropertyPricingService propertyPricingService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Property.Module)]
    public async Task<IActionResult> Index(PropertyPricingQueryViewModel query)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var accessiblePropertyIds = permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessiblePropertyIds;
        var viewModel = (await propertyPricingService.GetMatrixAsync(
            new GetPropertyPricingMatrixInput(
                query.PropertyId,
                query.Page,
                query.PageSize,
                accessiblePropertyIds))).ToViewModel();

        return View(viewModel);
    }
}

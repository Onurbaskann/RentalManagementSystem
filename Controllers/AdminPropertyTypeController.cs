using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.PropertyType;
using KiraTakip.Models.ViewModels;
using KiraTakip.Models.Common;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/PropertyType")]
public class AdminPropertyTypeController(IPropertyTypeService propertyTypeService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.PropertyType.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var list = await propertyTypeService.GetPagedListAsync(query);
        ViewBag.Query = query;
        return View(list);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.PropertyType.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSortOrder = (await propertyTypeService.GetMaxSortOrderAsync()) + 1;
        return View(new PropertyTypeFormViewModel { SortOrder = nextSortOrder, SupportsSingleUnit = true });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.PropertyType.Create)]
    public async Task<IActionResult> Create(PropertyTypeFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await propertyTypeService.CreateAsync(new CreateInput(
                model.Name,
                model.SortOrder,
                model.IsActive,
                model.SupportsSingleUnit,
                model.SupportsMultipleUnits
            ));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.PropertyType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await propertyTypeService.GetByIdAsync(id);
        if (entity == null) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.PropertyType.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] PropertyTypeFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid) return View(model);

        try
        {
            await propertyTypeService.UpdateAsync(id, new EditInput(
                model.Name,
                model.SortOrder,
                model.IsActive,
                model.SupportsSingleUnit,
                model.SupportsMultipleUnits
            ));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.PropertyType.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var entity = await propertyTypeService.GetByIdAsync(id);
        if (entity == null) return NotFound();

        await propertyTypeService.ToggleStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private static PropertyTypeFormViewModel ToFormVm(PropertyType e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        SortOrder = e.SortOrder,
        IsActive = e.IsActive,
        SupportsSingleUnit = e.SupportsSingleUnit,
        SupportsMultipleUnits = e.SupportsMultipleUnits
    };
}

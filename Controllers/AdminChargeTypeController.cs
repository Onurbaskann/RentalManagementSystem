using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.ChargeType;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/ChargeType")]
public class AdminChargeTypeController(IChargeTypeService chargeTypeService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.ChargeType.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await chargeTypeService.GetListAsync();
        return View(list);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.ChargeType.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSortOrder = await chargeTypeService.GetNextSortOrderAsync();
        return View(new ChargeTypeFormViewModel { SortOrder = nextSortOrder });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ChargeType.Create)]
    public async Task<IActionResult> Create(ChargeTypeFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await chargeTypeService.CreateAsync(
                new CreateInput(model.Name, model.Behavior, model.SortOrder, model.IsActive));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.ChargeType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await chargeTypeService.GetByIdAsync(id);
        if (entity == null) return NotFound();

        var vm = ToFormVm(entity);
        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ChargeType.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] ChargeTypeFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var entity = await chargeTypeService.GetByIdAsync(id);
        if (entity == null) return NotFound();

        // Sistem tiplerinde Davranış değiştirilemez
        if (entity.IsSystem)
            model.Behavior = entity.Behavior;

        if (!ModelState.IsValid)
        {
            model.IsSystem = entity.IsSystem;
            return View(model);
        }

        try
        {
            await chargeTypeService.UpdateAsync(id,
                new EditInput(model.Name, model.Behavior, model.SortOrder, model.IsActive));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            model.IsSystem = entity.IsSystem;
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ChargeType.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var entity = await chargeTypeService.GetByIdAsync(id);
        if (entity == null) return NotFound();

        await chargeTypeService.ToggleStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ChangeSortOrder/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.ChargeType.Edit)]
    public async Task<IActionResult> ChangeSortOrder(int id, int newSortOrder)
    {
        var entity = await chargeTypeService.GetByIdAsync(id);
        if (entity == null) return NotFound();
        await chargeTypeService.ChangeSortOrderAsync(id, newSortOrder);
        return RedirectToAction(nameof(Index));
    }

    private static ChargeTypeFormViewModel ToFormVm(KiraTakip.Models.Entities.ChargeType e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Behavior = e.Behavior,
        SortOrder = e.SortOrder,
        IsActive = e.IsActive,
        IsSystem = e.IsSystem
    };
}

using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/UnitType")]
public class AdminUnitTypeController(IUnitTypeService unitTypeService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.UnitType.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await unitTypeService.GetListAsync();
        return View(list);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.UnitType.Module)]
    public async Task<IActionResult> Create()
    {
        var vm = new UnitTypeFormViewModel
        {
            SortOrder = await unitTypeService.GetNextSortOrderAsync(),

            Usage = UnitTypeUsage.Rentable,
            ChargeTypeCandidates = await unitTypeService.GetChargeTypeCandidatesAsync()
        };
        return View(vm);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UnitType.Create)]
    public async Task<IActionResult> Create(UnitTypeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ChargeTypeCandidates = await unitTypeService.GetChargeTypeCandidatesAsync();
            return View(model);
        }

        try
        {
            await unitTypeService.CreateAsync(new CreateUnitTypeInput(
                model.Name,
                model.SortOrder,
                model.Usage,
                model.ChargeTypeId,
                model.IsActive));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            model.ChargeTypeCandidates = await unitTypeService.GetChargeTypeCandidatesAsync();
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.UnitType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var unitType = await unitTypeService.GetByIdAsync(new GetUnitTypeByIdInput(id));
        if (unitType == null) return NotFound();

        var vm = ToFormVm(unitType);
        vm.ChargeTypeCandidates = await unitTypeService.GetChargeTypeCandidatesAsync();

        return View(vm);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UnitType.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] UnitTypeFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            model.ChargeTypeCandidates = await unitTypeService.GetChargeTypeCandidatesAsync();
            return View(model);
        }

        try
        {
            await unitTypeService.UpdateAsync(new EditUnitTypeInput(
                id,
                model.Name,
                model.SortOrder,
                model.Usage,
                model.ChargeTypeId,
                model.IsActive));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            model.ChargeTypeCandidates = await unitTypeService.GetChargeTypeCandidatesAsync();
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.UnitType.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await unitTypeService.ToggleStatusAsync(new ToggleUnitTypeStatusInput(id));
        return RedirectToAction(nameof(Index));
    }

    private static UnitTypeFormViewModel ToFormVm(UnitTypeDetailDto unitType) => new()
    {
        Id = unitType.Id,
        Name = unitType.Name,
        SortOrder = unitType.SortOrder,
        Usage = unitType.Usage,
        ChargeTypeId = unitType.ChargeTypeId,
        IsActive = unitType.IsActive
    };
}

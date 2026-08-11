using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Models.Common;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/Sector")]
public class AdminSectorController(ISectorService sectorService) : Controller
{
    private const CategoryType Type = CategoryType.Sector;

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Sector.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var list = await sectorService.GetSectorsPagedAsync(query);
        ViewBag.Query = query;
        return View(list);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.Sector.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSira = await sectorService.GetNextOrderAsync();
        return View(new CategoryFormViewModel { Type = Type, Order = nextSira });
    }

    [HttpPost("Create")]
    [Authorize(Policy = PermissionCatalog.Sector.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        model.Type = Type;
        if (!ModelState.IsValid) return View(model);

        try
        {
            await sectorService.CreateAsync(new CreateSectorInput(model.Name, model.Order, model.IsActive));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.Sector.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await sectorService.GetByIdAsync(new GetSectorByIdInput(id));
        if (entity == null) return NotFound();

        return View(new CategoryFormViewModel
        {
            Id = entity.Id,
            Type = entity.Type,
            Name = entity.Name,
            Order = entity.Order,
            IsActive = entity.IsActive
        });
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Sector.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] CategoryFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        model.Type = Type;
        if (!ModelState.IsValid) return View(model);

        try
        {
            await sectorService.UpdateAsync(new EditSectorInput(id, model.Name, model.Order, model.IsActive));
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
    [Authorize(Policy = PermissionCatalog.Sector.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await sectorService.ToggleStatusAsync(new ToggleSectorStatusInput(id));
        return RedirectToAction(nameof(Index));
    }
}

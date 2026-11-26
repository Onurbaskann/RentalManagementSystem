using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Services.Interfaces.Spaces;
using KiraTakip.Web.Feedback;
using KiraTakip.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Web.Controllers;

[Authorize]
[Route("Admin/SpaceType")]
public class AdminSpaceTypeController(
    ISpaceTypeService spaceTypeService,
    ISpaceAttributeService spaceAttributeService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.SpaceType.Module)]
    public async Task<IActionResult> Index()
        => View(await spaceTypeService.GetListAsync());

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.SpaceType.Module)]
    public async Task<IActionResult> Create()
        => View(await PrepareAsync(new SpaceTypeFormViewModel()));

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.SpaceType.Create)]
    public async Task<IActionResult> Create(SpaceTypeFormViewModel model)
    {
        try
        {
            await spaceTypeService.CreateAsync(ToInput(model));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(await PrepareAsync(model));
        }

        TempData[FeedbackTempDataKeys.SuccessMessage] = "Alan türü eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.SpaceType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var type = (await spaceTypeService.GetListAsync()).FirstOrDefault(t => t.Id == id);
        if (type == null) return NotFound();

        return View(await PrepareAsync(new SpaceTypeFormViewModel
        {
            Id = type.Id,
            Code = type.Code,
            Name = type.Name,
            SortOrder = type.SortOrder,
            DefaultUsage = type.DefaultUsage,
            DefaultIsPreAllocatable = type.DefaultIsPreAllocatable,
            DefaultAttributeIds = type.DefaultAttributeIds.ToList()
        }));
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.SpaceType.Edit)]
    public async Task<IActionResult> Edit(int id, SpaceTypeFormViewModel model)
    {
        model.Id = id;
        try
        {
            await spaceTypeService.UpdateAsync(id, ToInput(model));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(await PrepareAsync(model));
        }

        TempData[FeedbackTempDataKeys.SuccessMessage] = "Alan türü güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.SpaceType.ToggleStatus)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await spaceTypeService.ToggleStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<SpaceTypeFormViewModel> PrepareAsync(SpaceTypeFormViewModel model)
    {
        model.AttributeCandidates = (await spaceAttributeService.GetListAsync())
            .Where(a => a.IsActive || model.DefaultAttributeIds.Contains(a.Id))
            .ToList();
        return model;
    }

    private static SpaceTypeInput ToInput(SpaceTypeFormViewModel model)
        => new(model.Code, model.Name, model.SortOrder, model.DefaultUsage, model.DefaultIsPreAllocatable, model.DefaultAttributeIds);
}

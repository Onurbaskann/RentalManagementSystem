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
[Route("Admin/SpaceAttribute")]
public class AdminSpaceAttributeController(ISpaceAttributeService spaceAttributeService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.SpaceAttribute.Module)]
    public async Task<IActionResult> Index()
        => View(await spaceAttributeService.GetListAsync());

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.SpaceAttribute.Module)]
    public IActionResult Create()
        => View(new SpaceAttributeFormViewModel());

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.SpaceAttribute.Create)]
    public async Task<IActionResult> Create(SpaceAttributeFormViewModel model)
    {
        try
        {
            await spaceAttributeService.CreateAsync(ToInput(model));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }

        TempData[FeedbackTempDataKeys.SuccessMessage] = "Alan özelliği eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.SpaceAttribute.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var attribute = (await spaceAttributeService.GetListAsync()).FirstOrDefault(a => a.Id == id);
        if (attribute == null) return NotFound();

        return View(new SpaceAttributeFormViewModel
        {
            Id = attribute.Id,
            Code = attribute.Code,
            Name = attribute.Name,
            DataType = attribute.DataType,
            Options = string.Join(Environment.NewLine, attribute.Options)
        });
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.SpaceAttribute.Edit)]
    public async Task<IActionResult> Edit(int id, SpaceAttributeFormViewModel model)
    {
        model.Id = id;
        try
        {
            await spaceAttributeService.UpdateAsync(id, ToInput(model));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }

        TempData[FeedbackTempDataKeys.SuccessMessage] = "Alan özelliği güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.SpaceAttribute.ToggleStatus)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await spaceAttributeService.ToggleStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private static SpaceAttributeInput ToInput(SpaceAttributeFormViewModel model)
        => new(model.Code, model.Name, model.DataType, model.Options);
}

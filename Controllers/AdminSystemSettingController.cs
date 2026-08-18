using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/SystemSetting")]
public class AdminSystemSettingController(ISystemSettingService systemSettingService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Parameter.Module)]
    public async Task<IActionResult> Index(
        [FromQuery] TableQuery query,
        CancellationToken cancellationToken)
    {
        ViewBag.Query = query;
        return View(await systemSettingService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.Parameter.Module)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var setting = await systemSettingService.GetByIdAsync(
            new GetSystemSettingInput(id),
            cancellationToken);
        return setting == null ? NotFound() : View(ToViewModel(setting));
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Parameter.Edit)]
    public async Task<IActionResult> Edit(
        int id,
        [FromForm] SystemSettingEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
            return await ReloadEditViewAsync(model, cancellationToken);

        try
        {
            await systemSettingService.UpdateAsync(
                new UpdateSystemSettingInput(id, model.Value),
                cancellationToken);
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(nameof(model.Value), exception.Message);
            return await ReloadEditViewAsync(model, cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadEditViewAsync(
        SystemSettingEditViewModel model,
        CancellationToken cancellationToken)
    {
        var setting = await systemSettingService.GetByIdAsync(
            new GetSystemSettingInput(model.Id),
            cancellationToken);
        if (setting == null) return NotFound();

        var viewModel = ToViewModel(setting);
        viewModel.Value = model.Value;
        return View(viewModel);
    }

    private static SystemSettingEditViewModel ToViewModel(SystemSettingListItemDto setting) => new()
    {
        Id = setting.Id,
        Key = setting.Key,
        DisplayName = setting.DisplayName,
        GroupDisplayName = setting.GroupDisplayName,
        Description = setting.Description,
        Value = setting.Value,
        InputKind = setting.InputKind,
        MinimumValue = setting.MinimumValue,
        MaximumValue = setting.MaximumValue
    };
}

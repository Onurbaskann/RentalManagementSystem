using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.DocumentType;
using KiraTakip.Models.Entities;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/DocumentType")]
public class AdminDocumentTypeController(IDocumentTypeService documentTypeService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.DocumentType.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await documentTypeService.GetListAsync();
        return View(list);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.DocumentType.Module)]
    public async Task<IActionResult> Create()
    {
        var maxSortOrder = await documentTypeService.GetMaxSortOrderAsync();
        var vm = new DocumentTypeFormViewModel
        {
            SortOrder = maxSortOrder + 1
        };
        return View(vm);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.DocumentType.Create)]
    public async Task<IActionResult> Create(DocumentTypeFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await documentTypeService.CreateAsync(new CreateInput(
                model.Name,
                model.Description,
                model.TargetEntity,
                model.Required,
                model.AllowedExtensions,
                model.MaxSizeMb,
                model.SortOrder,
                model.IsActive
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
    [Authorize(Policy = PermissionCatalog.DocumentType.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await documentTypeService.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();
        return View(ToFormVm(entity));
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.DocumentType.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] DocumentTypeFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var entity = await documentTypeService.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        if (entity.IsSystem)
            model.TargetEntity = entity.TargetEntity;

        if (!ModelState.IsValid)
        {
            model.IsSystem = entity.IsSystem;
            return View(model);
        }

        try
        {
            await documentTypeService.UpdateAsync(id, new EditInput(
                model.Name,
                model.Description,
                model.TargetEntity,
                model.Required,
                model.AllowedExtensions,
                model.MaxSizeMb,
                model.SortOrder,
                model.IsActive
            ));
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
    [Authorize(Policy = PermissionCatalog.DocumentType.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var entity = await documentTypeService.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        await documentTypeService.ToggleStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.DocumentType.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await documentTypeService.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted) return NotFound();

        await documentTypeService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private static DocumentTypeFormViewModel ToFormVm(DocumentType e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        TargetEntity = e.TargetEntity,
        Required = e.Required,
        AllowedExtensions = e.AllowedExtensions,
        MaxSizeMb = e.MaxSizeMb,
        SortOrder = e.SortOrder,
        IsActive = e.IsActive,
        IsSystem = e.IsSystem
    };
}

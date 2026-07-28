using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/TenantCategory")]
public class AdminTenantCategoryController(ITenantCategoryService tenantCategoryService) : Controller
{
    private const CategoryType Type = CategoryType.Tenant;

    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Module)]
    public async Task<IActionResult> Index()
    {
        var list = await tenantCategoryService.GetTenantCategoriesAsync();
        return View(list);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Module)]
    public async Task<IActionResult> Create()
    {
        var nextSira = await tenantCategoryService.GetNextOrderAsync();
        return View(new CategoryFormViewModel { Type = Type, Order = nextSira });
    }

    [HttpPost("Create")]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        model.Type = Type;
        if (!ModelState.IsValid) return View(model);

        try
        {
            await tenantCategoryService.CreateAsync(new CreateTenantCategoryInput(model.Name, model.Order, model.IsActive));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantCategory.Module)]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await tenantCategoryService.GetByIdAsync(new GetTenantCategoryByIdInput(id));
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
    [Authorize(Policy = PermissionCatalog.TenantCategory.Edit)]
    public async Task<IActionResult> Edit(int id, [FromForm] CategoryFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        
        model.Type = Type;
        if (!ModelState.IsValid) return View(model);

        try
        {
            await tenantCategoryService.UpdateAsync(new EditTenantCategoryInput(id, model.Name, model.Order, model.IsActive));
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
    [Authorize(Policy = PermissionCatalog.TenantCategory.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await tenantCategoryService.ToggleStatusAsync(new ToggleTenantCategoryStatusInput(id));
        return RedirectToAction(nameof(Index));
    }
}

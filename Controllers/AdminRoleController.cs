using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "System.Role")]
[Route("Admin/Roles")]
public class AdminRoleController(
    IRoleService roleService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var roller = await roleService.GetInternalRolesWithDetailsAsync();
        var model = roller.Select(r => new RoleListViewModel
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole,
            IsActive = r.IsActive,
            UserCount = r.UserCount,
            PermissionCount = r.PermissionCount
        }).ToList();

        return View(model);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var model = new RoleCreateViewModel();
        PopulatePermissions(model.Permissions, new List<string>());
        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulatePermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }

        try
        {
            var currentUserId = userManager.GetUserId(User)!;
            var rol = await roleService.CreateRoleAsync(new CreateRoleInput(model.Name, model.Description, currentUserId));
            await roleService.SetRolePermissionsAsync(new SetRolePermissionsInput(rol.Id, model.SelectedPermissions, currentUserId));
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(ex.Field, ex.Message);
            PopulatePermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var rol = await roleService.GetRoleByIdAsync(new GetRoleByIdInput(id));
        if (rol == null) return NotFound();

        var selected = await roleService.GetRolePermissionsAsync(new GetRolePermissionsInput(id));
        var model = new RoleEditViewModel
        {
            Id = rol.Id,
            Name = rol.Name,
            Description = rol.Description,
            IsSystemRole = rol.IsSystemRole,
            SelectedPermissions = selected
        };
        PopulatePermissions(model.Permissions, selected);
        return View(model);
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulatePermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }

        try
        {
            var currentUserId = userManager.GetUserId(User)!;
            await roleService.UpdateRoleAsync(new UpdateRoleInput(id, model.Name, model.Description, currentUserId));
            await roleService.SetRolePermissionsAsync(new SetRolePermissionsInput(id, model.SelectedPermissions, currentUserId));
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(ex.Field, ex.Message);
            PopulatePermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = userManager.GetUserId(User)!;
        await roleService.DeleteRoleAsync(new DeleteRoleInput(id, currentUserId));
        return RedirectToAction(nameof(Index));
    }

    private static void PopulatePermissions(List<PermissionGroupViewModel> target, List<string> selected)
    {
        target.Clear();
        target.AddRange(
            PermissionCatalog.AllModules
                .Where(m => !m.Path.StartsWith("Tenant."))
                .Select(m =>
                {
                    var items = new List<PermissionCheckboxViewModel>
                    {
                        new() { Value = m.Path, Label = m.AccessDisplayName, Selected = selected.Contains(m.Path) }
                    };
                    items.AddRange(m.ActionDefinitions.Select(action => new PermissionCheckboxViewModel
                    {
                        Value = action.Path,
                        Label = action.DisplayName,
                        Selected = selected.Contains(action.Path)
                    }));
                    return new PermissionGroupViewModel { GroupName = m.DisplayName, Permissions = items };
                })
        );
    }

}

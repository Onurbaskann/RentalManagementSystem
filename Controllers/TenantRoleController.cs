using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Models.Common;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Route("Tenant/Roles")]
public class TenantRoleController(
    ICurrentUserContext currentUser,
    IRoleService roleService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.Role.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var tenantId = currentUser.TenantId!.Value;
        var roles = await roleService.GetTenantRolesWithDetailsPagedAsync(
            new GetTenantRolesWithDetailsInput(tenantId), query);
        ViewBag.Query = query;
        return View(roles);
    }

    [HttpGet("Create")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.Role.Create)]
    public IActionResult Create()
    {
        var model = new TenantRoleFormViewModel();
        PopulateTenantPermissions(model.Permissions, []);

        return View(model);
    }

    [HttpPost("Create")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.Role.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenantRoleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateTenantPermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }

        var tenantId = currentUser.TenantId!.Value;

        try
        {
            await roleService.CreateTenantRoleAsync(new CreateTenantRoleInput(
                tenantId,
                model.Name,
                model.Description,
                model.SelectedPermissions,
                currentUser.UserId!));

            return RedirectToAction(nameof(Index));
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(ex.Field, ex.Message);
            PopulateTenantPermissions(model.Permissions, model.SelectedPermissions);

            return View(model);
        }
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.Role.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var tenantId = currentUser.TenantId!.Value;
        var role = await roleService.GetTenantRoleForEditAsync(
            new GetTenantRoleForEditInput(id, tenantId));
        var model = new TenantRoleFormViewModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            SelectedPermissions = role.SelectedPermissions
        };
        PopulateTenantPermissions(model.Permissions, role.SelectedPermissions);

        return View(model);
    }

    [HttpPost("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.Role.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TenantRoleFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            PopulateTenantPermissions(model.Permissions, model.SelectedPermissions);
            return View(model);
        }

        var tenantId = currentUser.TenantId!.Value;

        try
        {
            await roleService.UpdateTenantRoleAsync(new UpdateTenantRoleInput(
                id,
                tenantId,
                model.Name,
                model.Description,
                model.SelectedPermissions,
                currentUser.UserId!));

            return RedirectToAction(nameof(Index));
        }
        catch (BusinessValidationException ex)
        {
            ModelState.AddModelError(ex.Field, ex.Message);
            PopulateTenantPermissions(model.Permissions, model.SelectedPermissions);

            return View(model);
        }
    }

    [HttpPost("Delete/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.Role.Delete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tenantId = currentUser.TenantId!.Value;
        await roleService.DeleteTenantRoleAsync(
            new DeleteTenantRoleInput(id, tenantId, currentUser.UserId!));

        return RedirectToAction(nameof(Index));
    }

    private static void PopulateTenantPermissions(
        List<PermissionGroupViewModel> target,
        List<string> selected)
    {
        target.Clear();
        target.AddRange(
            PermissionCatalog.AllModules
                .Where(m => m.Path.StartsWith("Tenant."))
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
                    return new PermissionGroupViewModel
                    {
                        GroupName = m.DisplayName,
                        ParentGroupName = m.ParentGroupDisplayName,
                        Permissions = items
                    };
                })
        );
    }

}

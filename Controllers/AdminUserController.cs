using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = PermissionCatalog.User.Module)]
[Route("Admin/Users")]
public class AdminUserController(
    IAdminUserService adminUserService,
    ICurrentUserContext currentUserContext) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var data = await adminUserService.GetIndexAsync();
        var viewModel = new AdminUserIndexViewModel
        {
            InternalUsers = data.InternalUsers.Select(user => new AdminUserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive
            }).ToList(),
            TenantUsers = data.TenantUsers.Select(user => new AdminTenantUserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                TenantId = user.TenantId,
                TenantName = user.TenantName,
                RoleName = user.RoleName,
                IsActive = user.IsActive
            }).ToList(),
            PendingInvitations = data.PendingInvitations.Select(invitation => new AdminPendingInvitationViewModel
            {
                Id = invitation.Id,
                Email = invitation.Email,
                FullName = invitation.FullName,
                ExpiresAt = invitation.ExpiresAt
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var data = await adminUserService.GetEditDataAsync(new GetAdminUserEditDataInput(id, currentUserContext.UserId));
        if (data == null) return NotFound();

        return View(ToEditViewModel(data));
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, AdminUserEditViewModel model)
    {
        var data = await adminUserService.GetEditDataAsync(new GetAdminUserEditDataInput(id, currentUserContext.UserId));
        if (data == null) return NotFound();

        model.Id = data.Id;
        model.Email = data.Email;
        model.IsCurrentUser = data.IsCurrentUser;

        if (!ModelState.IsValid)
        {
            ApplyFormOptions(model, data.Options);
            return View(model);
        }

        try
        {
            await adminUserService.UpdateAsync(new UpdateAdminUserInput(
                id,
                currentUserContext.UserId,
                model.FullName,
                model.RoleId,
                model.HasAccessToAllProperties,
                model.SelectedPropertyIds ?? [],
                model.SelectedUnitIds ?? []));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            ApplyFormOptions(model, data.Options);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleActive/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        await adminUserService.ToggleActiveAsync(
            new ToggleAdminUserActiveInput(id, currentUserContext.UserId));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Invite")]
    public async Task<IActionResult> Invite()
    {
        var model = new AdminUserInviteViewModel();
        ApplyFormOptions(model, await adminUserService.GetFormOptionsAsync());

        return View(model);
    }

    [HttpPost("Invite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(AdminUserInviteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ApplyFormOptions(model, await adminUserService.GetFormOptionsAsync());
            return View(model);
        }

        var currentUserId = currentUserContext.UserId!;
        try
        {
            await adminUserService.SendInvitationAsync(new SendAdminUserInvitationInput(
                model.Email,
                model.FullName,
                model.RoleId,
                currentUserId,
                model.HasAccessToAllProperties,
                model.SelectedPropertyIds ?? [],
                model.SelectedUnitIds ?? []));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            ApplyFormOptions(model, await adminUserService.GetFormOptionsAsync());
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Invitation/Cancel/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelInvitation(int id)
    {
        await adminUserService.CancelInvitationAsync(new CancelAdminUserInvitationInput(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Invitation/Resend/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendInvitation(int id)
    {
        var currentUserId = currentUserContext.UserId!;
        await adminUserService.ResendInvitationAsync(new ResendAdminUserInvitationInput(id, currentUserId));
        return RedirectToAction(nameof(Index));
    }

    private static AdminUserEditViewModel ToEditViewModel(AdminUserEditDataDto data)
    {
        var model = new AdminUserEditViewModel
        {
            Id = data.Id,
            FullName = data.FullName,
            Email = data.Email,
            RoleId = data.RoleId,
            IsActive = data.IsActive,
            IsCurrentUser = data.IsCurrentUser,
            HasAccessToAllProperties = data.HasAccessToAllProperties,
            SelectedPropertyIds = data.SelectedPropertyIds,
            SelectedUnitIds = data.SelectedUnitIds
        };
        ApplyFormOptions(model, data.Options);

        return model;
    }

    private static void ApplyFormOptions(AdminUserEditViewModel model, AdminUserFormOptionsDto options)
    {
        model.Roles = options.Roles
            .Select(role => new AdminUserRoleOptionViewModel { Id = role.Id, Name = role.Name })
            .ToList();
        model.Properties = options.Properties
            .Select(property => new AdminUserPropertyOptionViewModel
            {
                PropertyId = property.Id,
                Name = property.Name,
                Location = property.Location,
                Selected = model.SelectedPropertyIds?.Contains(property.Id) ?? false
            }).ToList();
        model.Units = options.Units
            .Select(unit => new AdminUserUnitOptionViewModel
            {
                UnitId = unit.Id,
                Name = unit.Name,
                PropertyName = unit.PropertyName,
                Selected = model.SelectedUnitIds?.Contains(unit.Id) ?? false
            }).ToList();
    }

    private static void ApplyFormOptions(AdminUserInviteViewModel model, AdminUserFormOptionsDto options)
    {
        model.Roles = options.Roles
            .Select(role => new AdminUserRoleOptionViewModel { Id = role.Id, Name = role.Name })
            .ToList();
        model.Properties = options.Properties
            .Select(property => new AdminUserPropertyOptionViewModel
            {
                PropertyId = property.Id,
                Name = property.Name,
                Location = property.Location,
                Selected = model.SelectedPropertyIds?.Contains(property.Id) ?? false
            }).ToList();
        model.Units = options.Units
            .Select(unit => new AdminUserUnitOptionViewModel
            {
                UnitId = unit.Id,
                Name = unit.Name,
                PropertyName = unit.PropertyName,
                Selected = model.SelectedUnitIds?.Contains(unit.Id) ?? false
            }).ToList();
    }
}

using KiraTakip.Web.Authorization;
using KiraTakip.Web.Extensions;
using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models.Dtos.TenantUser;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Web.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Route("Tenant/Users")]
public class TenantUserController(
    ICurrentUserContext currentUser,
    ITenantUserService tenantUserService,
    IPermissionScopeProvider permissionScopeProvider,
    ICurrentUserPermissionService permissionService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.User.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var currentUserId = currentUser.UserId!;
        var data = await tenantUserService.GetTenantUsersPageAsync(
            new GetTenantUsersPageInput(currentUser.TenantId!.Value, query));

        return View(new TenantUserListViewModel
        {
            Users = new PagedResult<TenantUserListItemViewModel>
            {
                Items = data.Users.Items.Select(user => new TenantUserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    RoleName = user.RoleName,
                    RoleId = user.RoleId,
                    IsActive = user.IsActive,
                    IsCurrentUser = user.Id == currentUserId
                }).ToList(),
                Total = data.Users.Total,
                Page = data.Users.Page,
                Size = data.Users.Size
            },
            PendingInvitations = data.PendingInvitations.Select(invitation =>
                new TenantInvitationListItemViewModel
                {
                    Id = invitation.Id,
                    Email = invitation.Email,
                    FullName = invitation.FullName,
                    RoleName = invitation.RoleName,
                    SentAt = invitation.SentAt,
                    ExpiresAt = invitation.ExpiresAt
                }).ToList(),
            Query = query,
            CanInvite = await permissionService.HasPermissionAsync(PermissionCatalog.TenantPortal.System.User.Invite),
            CanEdit = await permissionService.HasPermissionAsync(PermissionCatalog.TenantPortal.System.User.Edit),
            CanDeactivate = await permissionService.HasPermissionAsync(PermissionCatalog.TenantPortal.System.User.Deactivate),
            CanResendInvite = await permissionService.HasPermissionAsync(PermissionCatalog.TenantPortal.System.User.ResendInvite),
            CanCancelInvite = await permissionService.HasPermissionAsync(PermissionCatalog.TenantPortal.System.User.CancelInvite)
        });
    }

    [HttpGet("Invite")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.User.Module)]
    public async Task<IActionResult> Invite()
    {
        var model = new TenantInvitationFormViewModel();
        await PopulateInviteOptionsAsync(model);
        return View(model);
    }

    [HttpPost("Invite")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.User.Invite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(TenantInvitationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateInviteOptionsAsync(model);
            return View(model);
        }

        try
        {
            await tenantUserService.SendInvitationAsync(new SendTenantInvitationInput(
                currentUser.TenantId!.Value,
                model.Email,
                model.FullName,
                model.RoleId,
                currentUser.UserId!,
                model.SpaceIds.Count > 0 ? model.SpaceIds : null));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateInviteOptionsAsync(model);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Invite/Cancel/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.User.CancelInvite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelInvite(int id)
    {
        await tenantUserService.CancelInvitationAsync(
            new CancelTenantInvitationInput(currentUser.TenantId!.Value, id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Invite/Resend/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.User.ResendInvite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendInvite(int id)
    {
        await tenantUserService.ResendInvitationAsync(new ResendTenantInvitationInput(
            currentUser.TenantId!.Value,
            id,
            currentUser.UserId!));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.User.Module)]
    public async Task<IActionResult> Edit(string id)
    {
        var data = await tenantUserService.GetTenantUserForEditAsync(
            new GetTenantUserForEditInput(
                currentUser.TenantId!.Value,
                id,
                currentUser.UserId!,
                GetAccessScope()));

        return View(ToEditViewModel(data));
    }

    [HttpPost("Edit/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.User.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, TenantUserEditViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            await PopulateEditOptionsAsync(model, id);
            return View(model);
        }

        try
        {
            await tenantUserService.EditTenantUserAsync(new EditTenantUserInput(
                currentUser.TenantId!.Value,
                id,
                model.FullName,
                model.RoleId,
                model.HasAccessToAllSpaces,
                model.SpaceIds,
                currentUser.UserId!,
                GetAccessScope()));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateEditOptionsAsync(model, id);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("ToggleStatus/{id}")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.System.User.Deactivate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        await tenantUserService.ToggleUserActiveAsync(
            new ToggleTenantUserActiveInput(
                currentUser.TenantId!.Value,
                id,
                currentUser.UserId!));
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateInviteOptionsAsync(TenantInvitationFormViewModel model)
    {
        var data = await tenantUserService.GetInviteDataAsync(
            new GetInviteDataInput(currentUser.TenantId!.Value, currentUser.UserId));
        model.Roles = data.Roles
            .Select(role => new RoleOptionViewModel { Id = role.Id, Name = role.Name })
            .ToList();
        model.Spaces = data.Spaces;
    }

    private async Task PopulateEditOptionsAsync(TenantUserEditViewModel model, string userId)
    {
        var data = await tenantUserService.GetTenantUserForEditAsync(
            new GetTenantUserForEditInput(
                currentUser.TenantId!.Value,
                userId,
                currentUser.UserId!,
                GetAccessScope()));
        model.Email = data.Email;
        model.IsActive = data.IsActive;
        model.Roles = data.Roles
            .Select(role => new RoleOptionViewModel { Id = role.Id, Name = role.Name })
            .ToList();
        model.LeaseSpaces = data.LeaseSpaces;
        model.ReservableSpaces = data.ReservableSpaces;
    }

    private static TenantUserEditViewModel ToEditViewModel(TenantUserEditDataDto data)
        => new()
        {
            Id = data.Id,
            FullName = data.FullName,
            Email = data.Email,
            IsActive = data.IsActive,
            RoleId = data.RoleId,
            HasAccessToAllSpaces = data.HasAccessToAllSpaces,
            SpaceIds = data.SelectedSpaceIds,
            Roles = data.Roles
                .Select(role => new RoleOptionViewModel { Id = role.Id, Name = role.Name })
                .ToList(),
            LeaseSpaces = data.LeaseSpaces,
            ReservableSpaces = data.ReservableSpaces
        };

    private SpaceAccessScope GetAccessScope() => permissionScopeProvider.SpaceScope;
}

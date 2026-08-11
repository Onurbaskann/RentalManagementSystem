using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "System.User")]
[Route("Admin/Tenants/{tenantId}/Users")]
public class AdminTenantUserController(
    ITenantUserService tenantUserService,
    UserManager<ApplicationUser> userManager
) : Controller
{
    private async Task PopulateRolesAndUnitsAsync(TenantInvitationFormViewModel model, int tenantId)
    {
        var data = await tenantUserService.GetInviteDataAsync(new GetInviteDataInput(tenantId));
        model.Roles = data.Roles.Select(r => new RoleOptionViewModel { Id = r.Id, Name = r.Name }).ToList();
        model.Units = data.Units;
        ViewBag.TenantId = tenantId;
        ViewBag.TenantName = data.TenantDisplayName;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int tenantId, [FromQuery] TableQuery query)
    {
        try
        {
            var data = await tenantUserService.GetTenantUsersPageAsync(
                new GetTenantUsersPageInput(tenantId, query));
            ViewBag.TenantId = tenantId;
            ViewBag.TenantName = data.TenantDisplayName;

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
                        IsActive = user.IsActive
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
                CanInvite = true,
                CanDeactivate = true
            });
        }
        catch (BusinessException exception) when (exception.ErrorType == ErrorType.NotFound)
        {
            return NotFound();
        }
    }

    [HttpPost("ToggleActive/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int tenantId, string id)
    {
        var currentUserId = userManager.GetUserId(User)!;
        await tenantUserService.ToggleUserActiveAsync(
            new ToggleTenantUserActiveInput(tenantId, id, currentUserId));
        return RedirectToAction(nameof(Index), new { tenantId });
    }

    [HttpPost("Invitation/Cancel/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelInvitation(int tenantId, int id)
    {
        await tenantUserService.CancelInvitationAsync(new CancelTenantInvitationInput(tenantId, id));
        return RedirectToAction(nameof(Index), new { tenantId });
    }

    [HttpPost("Invitation/Resend/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendInvitation(int tenantId, int id)
    {
        var currentUserId = userManager.GetUserId(User)!;
        await tenantUserService.ResendInvitationAsync(new ResendTenantInvitationInput(tenantId, id, currentUserId));
        return RedirectToAction(nameof(Index), new { tenantId });
    }

    [HttpGet("Invite")]
    public async Task<IActionResult> Invite(int tenantId)
    {
        try
        {
            var model = new TenantInvitationFormViewModel();
            await PopulateRolesAndUnitsAsync(model, tenantId);

            return View("Invite", model);
        }
        catch (BusinessException exception) when (exception.ErrorType == ErrorType.NotFound)
        {
            return NotFound();
        }
    }

    [HttpPost("Invite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(int tenantId, TenantInvitationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAndUnitsAsync(model, tenantId);

            return View("Invite", model);
        }

        var currentUserId = userManager.GetUserId(User)!;
        var unitIds = model.UnitIds.Count > 0 ? model.UnitIds : null;

        try
        {
            await tenantUserService.SendInvitationAsync(new SendTenantInvitationInput(
                tenantId,
                model.Email,
                model.FullName ?? string.Empty,
                model.RoleId,
                currentUserId,
                unitIds
            ));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateRolesAndUnitsAsync(model, tenantId);

            return View("Invite", model);
        }

        return RedirectToAction(nameof(Index), new { tenantId });
    }
}

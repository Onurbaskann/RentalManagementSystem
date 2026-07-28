using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
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
    public async Task<IActionResult> Index(int tenantId)
    {
        try
        {
            var data = await tenantUserService.GetTenantUsersListAsync(new GetTenantUsersListInput(tenantId));
            ViewBag.TenantId = tenantId;
            ViewBag.TenantName = data.TenantDisplayName;

            return View(data);
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

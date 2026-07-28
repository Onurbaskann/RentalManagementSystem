using KiraTakip.Authorization;
using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("ChargeReminder")]
public class ChargeReminderController(
    IChargeReminderService chargeReminderService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpPost("SendDebtorEmails")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.Notification.BorcHatirlatma)]
    public async Task<IActionResult> SendDebtorEmails()
    {
        await chargeReminderService.SendDebtRemindersAsync(GetScopeInput());
        return RedirectToAction(nameof(LeaseController.Index), "Lease");
    }

    private ChargeReminderScopeInput GetScopeInput()
        => permissionScopeProvider.GlobalAccess
            ? new ChargeReminderScopeInput()
            : new ChargeReminderScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}

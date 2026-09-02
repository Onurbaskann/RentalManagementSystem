using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("Admin/PaymentStoreRouting")]
public class AdminPaymentStoreRoutingController(IPaymentStoreRoutingService routingService) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.PaymentRouting.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query, [FromQuery] int? chargeTypeId)
    {
        var model = await BuildViewModelAsync(
            query,
            new PaymentStoreRoutingFormViewModel
            {
                ChargeTypeId = chargeTypeId.GetValueOrDefault(),
                Scope = PaymentRoutingScope.General
            },
            chargeTypeId);
        return View(model);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.PaymentRouting.Create)]
    public async Task<IActionResult> Save(PaymentStoreRoutingFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await BuildViewModelAsync(new TableQuery(), model, model.ChargeTypeId));

        try
        {
            await routingService.UpsertAsync(new UpsertPaymentStoreRoutingInput(
                model.ChargeTypeId,
                model.Scope,
                model.PropertyId,
                model.UnitId,
                model.StoreId));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(nameof(Index), await BuildViewModelAsync(new TableQuery(), model, model.ChargeTypeId));
        }

        TempData["Success"] = "Ödeme yönlendirmesi kaydedildi.";
        return RedirectToAction(nameof(Index), new { chargeTypeId = model.ChargeTypeId });
    }

    [HttpPost("Deactivate/{id}")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PermissionCatalog.PaymentRouting.Edit)]
    public async Task<IActionResult> Deactivate(int id)
    {
        await routingService.DeactivateOverrideAsync(id);
        TempData["Success"] = "Yönlendirme kaldırıldı. Bu kapsamdaki yeni ödemeler bir üst kapsamın mağazasına yönlendirilecek.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PaymentStoreRoutingIndexViewModel> BuildViewModelAsync(
        TableQuery query,
        PaymentStoreRoutingFormViewModel form,
        int? chargeTypeId)
    {
        var data = await routingService.GetManagementDataAsync(query);
        var option = chargeTypeId.HasValue
            ? data.ChargeTypes.FirstOrDefault(item => item.Id == chargeTypeId)
            : null;
        var guide = option is null
            ? null
            : new ChargeTypeSetupGuideViewModel(
                option.Id,
                option.Name,
                option.IsActive,
                await routingService.HasUsableDefaultAsync(option.Id));

        return new PaymentStoreRoutingIndexViewModel
        {
            Query = query,
            Data = data,
            Form = form,
            Guide = guide
        };
    }
}

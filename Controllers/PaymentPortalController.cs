using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[AllowAnonymous]
public class PaymentPortalController(
    IPaymentPortalService paymentPortalService) : Controller
{
    [Route("Payment/Portal")]
    public async Task<IActionResult> Index(
        [FromQuery] PaymentPortalRequestViewModel request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var failureReason = ModelState[nameof(request.Token)]?.Errors
                .FirstOrDefault()?.ErrorMessage
                ?? "Geçersiz veya süresi dolmuş ödeme bağlantısı.";

            return View(nameof(PaymentPortalViews.Invalid), failureReason);
        }

        var result = await paymentPortalService.GetAsync(
            new GetPaymentPortalInput(request.Token),
            cancellationToken);

        if (!result.Success)
            return View(nameof(PaymentPortalViews.Invalid), result.FailureReason);

        var viewModel = new TenantPaymentPortalViewModel
        {
            TenantName = result.TenantName,
            ChargeCards = result.Charges
                .Select(charge => new PaymentPortalChargeCardViewModel
                {
                    ChargeId = charge.ChargeId,
                    PropertyName = charge.PropertyName,
                    UnitName = charge.UnitName,
                    PeriodStart = charge.PeriodStart,
                    DueDate = charge.DueDate,
                    TotalAmount = charge.TotalAmount,
                    PaidAmount = charge.PaidAmount
                })
                .ToList(),
            DefaultSelectedId = result.Charges.FirstOrDefault()?.ChargeId ?? 0
        };

        if (viewModel.ChargeCards.Count == 0)
            return View(nameof(PaymentPortalViews.NoDebt), viewModel);

        return View(nameof(Index), viewModel);
    }

    private enum PaymentPortalViews
    {
        Invalid,
        NoDebt
    }
}

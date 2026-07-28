using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
public class PaymentController(
    IPaymentService paymentService,
    IDocumentService documentService,
    UserManager<ApplicationUser> userManager,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query, int? chargeId = null)
    {
        var accessScope = GetAccessScope();
        var pagedResult = await paymentService.GetPagedAsync(new GetPagedPaymentsInput(
            query,
            chargeId,
            accessScope.PropertyIds,
            accessScope.UnitIds));

        ViewBag.ChargeId = chargeId;
        ViewBag.Query = query;
        ViewBag.Status = query.Status ?? "tum";

        return View(pagedResult);
    }

    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Details(int id)
    {
        var payment = await paymentService.GetByIdAsync(
            new GetPaymentByIdInput(id, GetAccessScope()));
        if (payment == null) return NotFound();

        ViewBag.Documents = await documentService.GetListAsync(
            new GetDocumentsInput(DocumentOwnerType.Payment, id));
        ViewBag.DocumentTypes = await documentService.GetTypesAsync(
            new GetDocumentTypesInput(DocumentOwnerType.Payment));

        return View(payment);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Payment.Create)]
    public async Task<IActionResult> Create(int chargeId)
    {
        var charge = await paymentService.GetCreationContextAsync(
            new GetPaymentCreationContextInput(chargeId, GetAccessScope()));

        return View(new CreatePaymentViewModel
        {
            ChargeId = chargeId,
            Amount = charge.TotalAmount - charge.PaidAmount,
            Charge = charge
        });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePaymentViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            viewModel.Charge = await paymentService.GetCreationContextAsync(
                new GetPaymentCreationContextInput(viewModel.ChargeId, GetAccessScope()));

            return View(viewModel);
        }

        int paymentId;
        try
        {
            paymentId = await paymentService.CreateAsync(new CreatePaymentInput(
                viewModel.ChargeId,
                viewModel.PaymentDate,
                viewModel.Amount,
                viewModel.PaymentChannel,
                PaymentSourceType.Manual,
                viewModel.Description,
                userManager.GetUserId(User)!,
                GetAccessScope()));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            viewModel.Charge = await paymentService.GetCreationContextAsync(
                new GetPaymentCreationContextInput(viewModel.ChargeId, GetAccessScope()));
            return View(viewModel);
        }

        return RedirectToAction(nameof(Details), new { id = paymentId });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.Approve)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        await paymentService.ApproveAsync(new ApprovePaymentInput(
            id,
            userManager.GetUserId(User)!,
            GetAccessScope()));
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Payment.Reject)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectPaymentViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
                ?? "Red bilgileri geçersizdir.";
            throw new BusinessException(message);
        }

        await paymentService.RejectAsync(new RejectPaymentInput(
            viewModel.PaymentId,
            viewModel.Reason,
            GetAccessScope()));
        return RedirectToAction(nameof(Details), new { id = viewModel.PaymentId });
    }

    private PaymentAccessScopeInput GetAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new PaymentAccessScopeInput()
            : new PaymentAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}

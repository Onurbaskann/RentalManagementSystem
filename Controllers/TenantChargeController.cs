using KiraTakip.Authorization;
using KiraTakip.Extensions;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Authorize(Policy = PermissionCatalog.TenantPortal.Charge.Module)]
[Route("Tenant/Charges")]
public class TenantChargeController(
    IChargeService chargeService,
    IPaymentService paymentService,
    IDocumentService documentService,
    ICurrentUserContext currentUserContext,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] TenantChargeQueryViewModel query)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tenantId = currentUserContext.TenantId!.Value;
        var indexData = await chargeService.GetTenantChargeIndexAsync(
            new GetTenantChargeIndexInput(
                tenantId,
                DateTime.Today,
                new TenantChargeQueryInput(
                    query.Page,
                    query.Size,
                    query.Q,
                    query.Status,
                    query.UnitId,
                    query.Source,
                    query.Year),
                ScopePropertyIds(),
                ScopeUnitIds()));

        var viewModel = new TenantChargeIndexViewModel
        {
            Charges = indexData.Charges,
            Query = query,
            Status = query.Status ?? "tum",
            TotalChargeAmount = indexData.TotalChargeAmount,
            CollectedAmount = indexData.CollectedAmount,
            RemainingDebtAmount = indexData.RemainingDebtAmount,
            OverdueRemainingAmount = indexData.OverdueRemainingAmount,
            Units = indexData.Units,
            AvailableYears = indexData.AvailableYears,
            CanReportPayment = User.HasModuleAccess(
                PermissionCatalog.TenantPortal.Payment.Module)
        };

        return View(viewModel);
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var charge = await chargeService.GetTenantDetailsAsync(
            new GetTenantChargeDetailsInput(
                id,
                currentUserContext.TenantId!.Value,
                ScopePropertyIds(),
                ScopeUnitIds()));
        var paymentDocuments = await documentService.GetListsAsync(
            new GetDocumentsForOwnersInput(
                DocumentOwnerType.Payment,
                charge.Allocations.Select(allocation => allocation.Id).ToList()));

        return View(new TenantChargeDetailsViewModel
        {
            Charge = charge,
            PaymentDocuments = paymentDocuments,
            CanReportPayment = User.HasModuleAccess(
                PermissionCatalog.TenantPortal.Payment.Module)
        });
    }

    [HttpPost("ReportPayment")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.Payment.Module)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportPayment(TenantChargePaymentFormViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? "Ödeme bilgileri geçersizdir.";
            throw new BusinessException(message);
        }

        using var stream = new MemoryStream();
        await viewModel.Receipt!.CopyToAsync(stream);

        await paymentService.ReportTenantPaymentAsync(new ReportTenantPaymentInput(
            currentUserContext.TenantId!.Value,
            viewModel.ChargeId,
            viewModel.PaymentDate,
            viewModel.Amount,
            viewModel.PaymentChannel,
            viewModel.Description,
            currentUserContext.UserId!,
            viewModel.Receipt.FileName,
            viewModel.Receipt.ContentType,
            stream.ToArray(),
            new PaymentAccessScopeInput(ScopePropertyIds(), ScopeUnitIds()),
            ChargeLineItemId: viewModel.ChargeLineItemId));

        return RedirectToAction(nameof(Details), new { id = viewModel.ChargeId });
    }

    private IReadOnlyList<int>? ScopePropertyIds()
        => permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessiblePropertyIds;

    private IReadOnlyList<int>? ScopeUnitIds()
        => permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessibleUnitIds;}

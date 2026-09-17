using KiraTakip.Web.Authorization;
using KiraTakip.Authorization;
using KiraTakip.Web.Extensions;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KiraTakip.Models.Dtos.Charge;
using KiraTakip.Models.Dtos.Document;
using KiraTakip.Models.Dtos.Payment;
using KiraTakip.Models.Dtos.OnlinePayment;
using KiraTakip.Web.Feedback;

namespace KiraTakip.Web.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Authorize(Policy = PermissionCatalog.TenantPortal.Charge.Module)]
[Route("Tenant/Charges")]
public class TenantChargeController(
    IChargeService chargeService,
    IPaymentService paymentService,
    IOnlinePaymentService onlinePaymentService,
    IDocumentService documentService,
    ICurrentUserContext currentUserContext,
    IPermissionScopeProvider permissionScopeProvider,
    ICurrentUserPermissionService permissionService) : Controller
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
            CanReportPayment = await permissionService.HasModuleAccessAsync(
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
            CanReportPayment = await permissionService.HasModuleAccessAsync(
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

    [HttpPost("StartOnlinePayment")]
    [Authorize(Policy = PermissionCatalog.TenantPortal.Payment.Module)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartOnlinePayment(int chargeId, int chargeLineItemId, decimal amount)
    {
        var result = await onlinePaymentService.InitiateAsync(new InitiateOnlinePaymentInput(
            currentUserContext.TenantId!.Value,
            chargeId,
            chargeLineItemId,
            amount,
            currentUserContext.UserId!,
            new PaymentAccessScopeInput(ScopePropertyIds(), ScopeUnitIds())));

        if (string.IsNullOrWhiteSpace(result.RedirectUrl))
        {
            TempData["Error"] = "Ödeme başlatılamadı. Lütfen tekrar deneyin.";
            return RedirectToAction(nameof(Details), new { id = chargeId });
        }

        return Redirect(result.RedirectUrl);
    }

    /// <summary>
    /// Paratika'nın hosted ödeme sayfasından RETURNURL ile geri dönüşünü karşılar. Bu, Paratika'nın
    /// kendi tarayıcı POST-redirect'idir — bizim sayfamızdan gönderilen bir form değil; bu yüzden
    /// [ValidateAntiForgeryToken] BİLİNÇLİ OLARAK eklenmedi. Güvenlik: bu action gelen hiçbir alana
    /// (tutar/durum) güvenmez, yalnızca merchantPaymentId'yi bir SORGU ANAHTARI olarak okur — asıl
    /// karar OnlinePaymentService.CompleteAsync'in yaptığı authenticated QUERYTRANSACTION sonucuna
    /// dayanır (İç Faz 7 planı). POST body'deki gerçek alan adı Paratika sandbox'ında doğrulanana
    /// kadar hem küçük hem büyük harfli varyant denenir.
    /// </summary>
    [HttpPost("OnlinePaymentReturn")]
    public async Task<IActionResult> OnlinePaymentReturn()
    {
        var merchantPaymentId = Request.Form["merchantPaymentId"].FirstOrDefault()
            ?? Request.Form["MERCHANTPAYMENTID"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(merchantPaymentId))
        {
            TempData["Error"] = "Ödeme sonucu okunamadı. Lütfen tahakkuk sayfasından durumu kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        var result = await onlinePaymentService.CompleteAsync(new CompleteOnlinePaymentInput(
            merchantPaymentId,
            currentUserContext.TenantId!.Value));

        if (result.Status == OnlinePaymentTransactionStatus.Approved)
        {
            TempData[FeedbackTempDataKeys.OperationSucceeded] = true;
            TempData[FeedbackTempDataKeys.SuccessMessage] = "Ödemeniz başarıyla alındı.";
        }
        else
        {
            TempData["Error"] = result.Status switch
            {
                OnlinePaymentTransactionStatus.Failed => "Ödeme başarısız oldu.",
                OnlinePaymentTransactionStatus.Cancelled => "Ödeme iptal edildi.",
                _ => "Ödemeniz işleniyor, kısa süre sonra tekrar kontrol edin."
            };
        }

        return RedirectToAction(nameof(Details), new { id = result.ChargeId });
    }

    private IReadOnlyList<int>? ScopePropertyIds()
        => permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessiblePropertyIds;

    private IReadOnlyList<int>? ScopeUnitIds()
        => permissionScopeProvider.GlobalAccess
            ? null
            : permissionScopeProvider.AccessibleUnitIds;
}

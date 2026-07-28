using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
[Route("BankTransaction")]
public class BankTransactionController(
    IBankTransactionService bankTransactionService,
    IPaymentService paymentService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    [Authorize(Policy = PermissionCatalog.Payment.Module)]
    public async Task<IActionResult> Index([FromQuery] TableQuery query)
    {
        var pagedResult = await bankTransactionService.GetPagedAsync(query);

        ViewBag.Query = query;
        ViewBag.Status = query.Status ?? "tum";

        return View(pagedResult);
    }

    [HttpGet("Import")]
    [Authorize(Policy = PermissionCatalog.Payment.ImportBankStatement)]
    public IActionResult Import()
    {
        return View(new BankTransactionImportViewModel());
    }

    [HttpPost("Import")]
    [Authorize(Policy = PermissionCatalog.Payment.ImportBankStatement)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(BankTransactionImportViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await using var stream = model.File!.OpenReadStream();
            await bankTransactionService.ImportAsync(new ImportBankTransactionsInput(stream, model.BankCode));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("SelectMatch/{id}")]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    public async Task<IActionResult> SelectMatch(int id)
    {
        var bankTransaction = await bankTransactionService.GetByIdAsync(new GetBankTransactionByIdInput(id));
        if (bankTransaction == null) return NotFound();

        var accessScope = GetPaymentAccessScope();
        var paymentCandidates = await bankTransactionService.GetPaymentCandidatesAsync(
            new GetBankTransactionPaymentCandidatesInput(
                id,
                accessScope.PropertyIds,
                accessScope.UnitIds));

        return View(new BankTransactionMatchSelectionViewModel
        {
            BankTransaction = bankTransaction,
            PaymentCandidates = paymentCandidates,
        });
    }

    [HttpGet("SelectForPayment/{id}")]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    public async Task<IActionResult> SelectForPayment(int id)
    {
        var payment = await paymentService.GetByIdAsync(
            new GetPaymentByIdInput(id, GetPaymentAccessScope()));
        if (payment == null) return NotFound();

        var transactionCandidates = await bankTransactionService.GetTransactionCandidatesAsync(
            new GetBankTransactionCandidatesInput(id));
        return View(new SelectPaymentBankTransactionViewModel
        {
            Payment = payment,
            TransactionCandidates = transactionCandidates
        });
    }

    [HttpPost("Match")]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Match(BankTransactionMatchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
                ?? "Girilen eşleştirme bilgileri geçersiz.";
            throw new BusinessException(message);
        }

        var accessScope = GetPaymentAccessScope();
        await bankTransactionService.MatchAsync(new MatchBankTransactionInput(
            model.PaymentId,
            model.BankTransactionId,
            accessScope.PropertyIds,
            accessScope.UnitIds));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Unmatch")]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unmatch(int matchId)
    {
        await bankTransactionService.UnmatchAsync(new UnmatchBankTransactionInput(
            matchId,
            GetPaymentAccessScope().PropertyIds,
            GetPaymentAccessScope().UnitIds));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("RemovePaymentMatch")]
    [Authorize(Policy = PermissionCatalog.Payment.MatchBankTransaction)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePaymentMatch(int matchId)
    {
        var paymentId = await bankTransactionService.UnmatchAsync(new UnmatchBankTransactionInput(
            matchId,
            GetPaymentAccessScope().PropertyIds,
            GetPaymentAccessScope().UnitIds));

        return RedirectToAction(
            nameof(PaymentController.Details),
            "Payment",
            new { id = paymentId });
    }

    private PaymentAccessScopeInput GetPaymentAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new PaymentAccessScopeInput()
            : new PaymentAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}

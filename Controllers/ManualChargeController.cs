using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize]
public class ManualChargeController(
    IManualChargeService manualChargeService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [Authorize(Policy = PermissionCatalog.ManualCharge.Module)]
    public async Task<IActionResult> Index(
        [FromQuery] TableQuery query,
        string? relation,
        int? leaseId)
    {
        var accessScope = GetAccessScope();

        var manualCharges = await manualChargeService.GetPageAsync(new GetManualChargesPageInput(
            query,
            accessScope.PropertyIds,
            relation,
            leaseId,
            accessScope.UnitIds));

        ViewBag.CancelledCount = await manualChargeService.GetCancelledCountAsync(
            new GetCancelledManualChargeCountInput(
                accessScope.PropertyIds,
                accessScope.UnitIds));

        ViewBag.Query = query;
        ViewBag.Status = query.Status ?? "tum";
        ViewBag.Relation = relation ?? "";
        ViewBag.LeaseId = leaseId;

        return View(manualCharges);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.ManualCharge.Create)]
    public async Task<IActionResult> Create(int? leaseId)
    {
        var viewModel = new CreateManualChargeViewModel { DueDate = DateTime.Today };
        await PopulateOptionsAsync(viewModel);

        if (leaseId.HasValue)
        {
            var lease = viewModel.ActiveLeases.FirstOrDefault(item => item.Id == leaseId.Value);
            if (lease != null)
            {
                viewModel.LeaseId = lease.Id;
                viewModel.TenantId = lease.TenantId;
                viewModel.UnitId = lease.UnitId;
            }
        }

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ManualCharge.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateManualChargeViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        try
        {
            await manualChargeService.CreateAsync(new CreateManualChargeInput(
                viewModel.TenantId,
                viewModel.LeaseId,
                viewModel.UnitId,
                viewModel.ChargeTypeId,
                viewModel.Description,
                viewModel.Amount,
                viewModel.IsVatApplied,
                viewModel.VatRate,
                viewModel.DueDate,
                viewModel.Note,
                GetAccessScope()));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateOptionsAsync(viewModel);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ManualCharge.Cancel)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancelManualChargeViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            var message = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error))
                ?? "İptal nedeni geçersizdir.";
            throw new BusinessException(message);
        }

        await manualChargeService.CancelAsync(new CancelManualChargeInput(
            id,
            viewModel.Reason,
            GetAccessScope()));

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateOptionsAsync(CreateManualChargeViewModel viewModel)
    {
        var accessScope = GetAccessScope();

        viewModel.ActiveLeases = await manualChargeService.GetActiveLeasesAsync(
            new GetActiveManualChargeLeasesInput(accessScope));
        viewModel.ChargeTypes = await manualChargeService.GetManualChargeTypesAsync();
        viewModel.Units = await manualChargeService.GetAllUnitsAsync(
            new GetManualChargeUnitsInput(
                accessScope.PropertyIds,
                accessScope.UnitIds));
    }

    private ManualChargeAccessScopeInput GetAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new ManualChargeAccessScopeInput()
            : new ManualChargeAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);
}

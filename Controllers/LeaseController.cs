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

[Authorize]
public class LeaseController(
    ILeaseService leaseService,
    IPropertyService propertyService,
    ITenantService tenantService,
    IStatisticsService statisticsService,
    IChargeService chargeService,
    IRateHierarchyService rateHierarchyService,
    IChargeReminderService chargeReminderService,
    IDocumentService documentService,
    IPermissionScopeProvider permissionScopeProvider,
    IPermissionScopeCache permissionScopeCache,
    ICurrentUserContext currentUserContext) : Controller
{
    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> Index(string? filter)
    {
        var leases = await leaseService.GetAllAsync(
            new GetLeasesInput(
                filter,
                permissionScopeProvider.GlobalAccess ? null : permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.GlobalAccess ? null : permissionScopeProvider.AccessibleUnitIds));

        ViewBag.DebtorCount = await chargeReminderService.GetDebtorCountAsync(
            permissionScopeProvider.GlobalAccess
                ? new ChargeReminderScopeInput()
                : new ChargeReminderScopeInput(
                    permissionScopeProvider.AccessiblePropertyIds,
                    permissionScopeProvider.AccessibleUnitIds));
        ViewBag.Filter = filter ?? "tum";

        return View(leases);
    }

    [Authorize(Policy = PermissionCatalog.Lease.Module)]
    public async Task<IActionResult> Details(int id)
    {
        var leaseDetails = await leaseService.GetDetailsAsync(new GetLeaseDetailsInput(id));
        if (leaseDetails == null) return NotFound();
        if (!IsInScope(leaseDetails.PropertyId, leaseDetails.UnitId)) return Forbid();

        var previousLeases = await leaseService.GetByUnitAsync(new GetLeasesByUnitInput(leaseDetails.UnitId));
        var tenantLeases = await leaseService.GetByTenantAsync(new GetLeasesByTenantInput(leaseDetails.TenantId));
        var lease = BuildLeaseForStatistics(leaseDetails);

        var viewModel = new LeaseDetailsViewModel
        {
            Lease = leaseDetails,
            RemainingDays = statisticsService.GetRemainingDays(lease),
            MonthlyAmount = await statisticsService.GetMonthlyAmountAsync(lease),
            AnnualAmount = await statisticsService.GetAnnualAmountAsync(lease),
            IsActive = statisticsService.IsActive(lease),
            DurationPercentage = statisticsService.GetDurationPercentage(lease),
            UnitStatus = statisticsService.GetUnitStatus(lease.Unit),
            PreviousLeases = previousLeases.Where(item => item.Id != id).ToList(),
            TenantOtherLeases = tenantLeases
                .Where(item => item.Id != id
                    && item.UnitId != leaseDetails.UnitId
                    && IsInScope(item.PropertyId, item.UnitId))
                .ToList(),
            EffectiveVatRate = leaseDetails.LeaseRateOverrides
                .FirstOrDefault(rate => rate.ChargeTypeBehavior == ChargeTypeBehavior.MonthlyFixed)?.VatRate ?? 20m
        };

        var hasRegeneratePermission = User.HasPermission(PermissionCatalog.Charge.Regenerate);
        if (User.HasPermission(PermissionCatalog.Payment.Module) || hasRegeneratePermission)
        {
            viewModel.HasPaymentAccess = User.HasPermission(PermissionCatalog.Payment.Module);
            await chargeService.UpdateDelaysAsync();

            viewModel.Charges = await chargeService.GetListAsync(new GetChargesInput(LeaseId: id));
        }

        PopulateRegenerationDefaults(viewModel);

        var currentCharge = await chargeService.GetCurrentLeaseChargeAsync(
            new GetCurrentLeaseChargeInput(id, DateTime.Today));
        viewModel.CurrentLineItems = currentCharge.LineItems;
        viewModel.CurrentLineItemPeriod = currentCharge.Period;

        viewModel.ParentRate = await rateHierarchyService.GetParentForAsync(new GetParentRateInput(
            RateHierarchyLayer.Lease,
            leaseDetails.PropertyId,
            leaseDetails.UnitId,
            leaseDetails.TenantCategoryId,
            leaseDetails.StartDate.Year));

        var deposits = await leaseService.GetDepositsAsync(new GetLeaseDepositsInput([id]));
        viewModel.DepositAmount = deposits.TryGetValue(id, out var deposit) ? deposit : null;
        viewModel.Documents = await documentService.GetListAsync(
            new GetDocumentsInput(DocumentOwnerType.Lease, id));
        viewModel.DocumentTypes = await documentService.GetTypesAsync(
            new GetDocumentTypesInput(DocumentOwnerType.Lease));

        var manualChargeSummary = await chargeService.GetManualLeaseChargeSummaryAsync(
            new GetManualLeaseChargeSummaryInput(id));
        if (manualChargeSummary.Count > 0)
        {
            ViewBag.ManualChargeCount = manualChargeSummary.Count;
            ViewBag.ManualChargeRemaining = manualChargeSummary.RemainingAmount;
        }

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = PermissionCatalog.Lease.Create)]
    public async Task<IActionResult> Create(int? unitId)
    {
        var viewModel = new CreateLeaseViewModel
        {
            UnitId = unitId
        };
        await PopulateCreateOptionsAsync(viewModel);

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Create)]
    public async Task<IActionResult> Create(CreateLeaseViewModel viewModel)
    {
        await PopulateCreateOptionsAsync(viewModel);

        foreach (var documentType in viewModel.DocumentTypes.Where(documentType => documentType.Required))
        {
            var file = Request.Form.Files.GetFile($"dosya_{documentType.Id}");
            if (file == null || file.Length == 0)
                ModelState.AddModelError(
                    $"dosya_{documentType.Id}",
                    $"'{documentType.Name}' belgesi zorunludur.");
        }

        if (!ModelState.IsValid) return View(viewModel);

        Lease lease;
        try
        {
            lease = await leaseService.CreateAsync(new CreateLeaseInput(
                viewModel.UnitId!.Value,
                viewModel.TenantId,
                viewModel.StartDate,
                viewModel.EndDate,
                viewModel.DueDateRuleType,
                viewModel.DueDay,
                viewModel.Description,
                BuildRateOverrideInputs(viewModel.LeaseLineItems),
                BuildAccessScope()));
        }
        catch (BusinessValidationException exception)
        {
            ModelState.AddModelError(exception.Field, exception.Message);
            await PopulateCreateOptionsAsync(viewModel);
            return View(viewModel);
        }

        await UploadDocumentsAsync(lease.Id, viewModel.DocumentTypes);
        return RedirectToAction(nameof(Details), new { id = lease.Id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Extend)]
    public async Task<IActionResult> Extend(int id, ExtendLeaseViewModel viewModel)
    {
        ThrowIfModelStateInvalid();

        await leaseService.ExtendAsync(new ExtendLeaseInput(
            id,
            viewModel.NewEndDate,
            viewModel.ApplyVat,
            viewModel.VatRate ?? 20,
            viewModel.InflationRate,
            viewModel.Description,
            viewModel.UpdateRate,
            User.HasPermission(PermissionCatalog.Lease.OverrideRate),
            BuildRateOverrideInputs(viewModel.LeaseLineItems),
            BuildAccessScope()));

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Edit)]
    public async Task<IActionResult> UpdateDueDate(int id, UpdateLeaseDueDateViewModel viewModel)
    {
        ThrowIfModelStateInvalid();

        await leaseService.UpdateDueDateAsync(new UpdateLeaseDueDateInput(
            id,
            viewModel.RuleType,
            viewModel.DueDay,
            viewModel.Description,
            BuildAccessScope()));

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Terminate)]
    public async Task<IActionResult> Terminate(int id, TerminateLeaseViewModel viewModel)
    {
        ThrowIfModelStateInvalid();

        await leaseService.TerminateAsync(new TerminateLeaseInput(
            id,
            viewModel.TerminationDate,
            viewModel.TerminationReason,
            viewModel.Description,
            BuildAccessScope()));

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Charge.Regenerate)]
    public async Task<IActionResult> Regenerate(int id, RegenerateLeaseViewModel viewModel)
    {
        ThrowIfModelStateInvalid();

        await leaseService.RegenerateAsync(new RegenerateLeaseInput(
            id,
            viewModel.StartDate,
            viewModel.UpdateRate,
            User.HasPermission(PermissionCatalog.Lease.OverrideRate),
            BuildRateOverrideInputs(viewModel.LeaseLineItems ?? []),
            BuildAccessScope()));

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.Lease.Edit)]
    public IActionResult CalculateInflationAndVat(CalculateRentIncreaseViewModel viewModel)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = statisticsService.CalculateRentIncrease(new CalculateRentIncreaseInput(
            viewModel.CurrentAmount,
            viewModel.InflationRate,
            viewModel.ApplyVat,
            viewModel.VatRate));

        return Json(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetDefaultLineItems(GetDefaultLeaseLineItemsViewModel query)
    {
        if (!User.HasPermission(PermissionCatalog.Lease.Create)
            && !User.HasPermission(PermissionCatalog.Lease.Extend)
            && !User.HasPermission(PermissionCatalog.Charge.Regenerate))
            return Forbid();

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var previews = await leaseService.GetDefaultLineItemsAsync(
            new ComposeLeaseLineItemsInput(
                query.UnitId,
                query.TenantId,
                query.StartDate,
                query.LeaseId,
                await BuildCurrentUserAccessScopeAsync()));

        var result = previews.Select(preview => new LeaseLineItemInputDto
        {
            ChargeTypeId = preview.ChargeTypeId,
            ChargeTypeName = preview.ChargeTypeName,
            ChargeTypeCode = preview.ChargeTypeCode,
            Behavior = preview.Behavior,
            DefaultAmount = preview.Amount,
            Amount = preview.Amount,
            UnitValue = preview.UnitValue,
            DefaultUnitValue = preview.UnitValue,
            VatRate = preview.KdvRate,
            CalculationMethod = preview.CalculationMethod,
            SourceType = preview.SourceType.ToString(),
            IsRateFound = preview.IsRateFound,
            IsUserModified = false
        }).ToList();

        return Json(result);
    }

    private void ThrowIfModelStateInvalid()
    {
        if (ModelState.IsValid) return;

        var message = string.Join(" | ", ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .Where(error => !string.IsNullOrWhiteSpace(error)));
        throw new BusinessException(
            string.IsNullOrWhiteSpace(message) ? "Girilen bilgiler geçersiz." : message);
    }

    private static Lease BuildLeaseForStatistics(LeaseDetailDto details)
        => new()
        {
            Id = details.Id,
            TenantId = details.TenantId,
            UnitId = details.UnitId,
            StartDate = details.StartDate,
            EndDate = details.EndDate,
            Status = details.Status,
            TerminationDate = details.TerminationDate,
            Unit = new Unit
            {
                Id = details.UnitId,
                Area = details.UnitArea,
                PropertyId = details.PropertyId
            }
        };

    private static List<LeaseRateOverrideInput> BuildRateOverrideInputs(
        IEnumerable<LeaseLineItemInputDto> lineItems)
        => lineItems
            .Where(lineItem => lineItem.IsUserModified)
            .Select(lineItem => new LeaseRateOverrideInput(
                lineItem.ChargeTypeId,
                lineItem.UnitValue,
                lineItem.CalculationMethod,
                lineItem.VatRate))
            .ToList();

    private async Task PopulateCreateOptionsAsync(CreateLeaseViewModel viewModel)
    {
        var accessScope = BuildAccessScope();
        viewModel.AvailableUnits = await propertyService.GetAvailableUnitsAsync(
            new GetAvailableUnitsInput(accessScope.PropertyIds, accessScope.UnitIds));
        viewModel.Tenants = await tenantService.GetAllAsync(
            new GetTenantsInput(accessScope.PropertyIds, accessScope.UnitIds));
        viewModel.DocumentTypes = await documentService.GetTypesAsync(
            new GetDocumentTypesInput(DocumentOwnerType.Lease));

        ViewBag.UnitAreas = System.Text.Json.JsonSerializer.Serialize(
            viewModel.AvailableUnits.ToDictionary(unit => unit.Id, unit => (double)unit.Area));
    }

    private LeaseAccessScopeInput BuildAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new LeaseAccessScopeInput()
            : new LeaseAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);

    private async Task<LeaseAccessScopeInput> BuildCurrentUserAccessScopeAsync()
    {
        var scope = await permissionScopeCache.GetAsync(currentUserContext.UserId!);
        return scope.GlobalAccess
            ? new LeaseAccessScopeInput()
            : new LeaseAccessScopeInput(scope.PropertyIds, scope.UnitIds);
    }

    private bool IsInScope(int propertyId, int unitId)
        => permissionScopeProvider.GlobalAccess
            || permissionScopeProvider.AccessiblePropertyIds.Contains(propertyId)
            || permissionScopeProvider.AccessibleUnitIds.Contains(unitId);

    private static void PopulateRegenerationDefaults(LeaseDetailsViewModel viewModel)
    {
        if (viewModel.Charges.Any())
        {
            var firstUnpaidCharge = viewModel.Charges
                .Where(charge => charge.SourceType == ChargeSourceType.Lease
                    && charge.Status != ChargeStatus.Paid
                    && charge.PaidAmount == 0)
                .OrderBy(charge => charge.PeriodStart)
                .FirstOrDefault();
            viewModel.DefaultRegenerationStartDate =
                firstUnpaidCharge?.PeriodStart ?? DateTime.Today;

            var lastPaidCharge = viewModel.Charges
                .Where(charge => charge.SourceType == ChargeSourceType.Lease
                    && (charge.Status == ChargeStatus.Paid || charge.PaidAmount > 0))
                .OrderByDescending(charge => charge.PeriodStart)
                .FirstOrDefault();
            viewModel.LastPaidPeriod = lastPaidCharge?.PeriodStart;
            viewModel.UnpaidChargeCount = viewModel.Charges.Count(charge =>
                charge.SourceType == ChargeSourceType.Lease
                && charge.Status != ChargeStatus.Paid
                && charge.PaidAmount == 0);
        }
        else
        {
            viewModel.DefaultRegenerationStartDate = DateTime.Today;
        }
    }

    private async Task UploadDocumentsAsync(int leaseId, IEnumerable<DocumentType> documentTypes)
    {
        foreach (var documentType in documentTypes)
        {
            var file = Request.Form.Files.GetFile($"dosya_{documentType.Id}");
            if (file == null || file.Length == 0) continue;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            await documentService.UploadAsync(new UploadDocumentInput(
                DocumentOwnerType.Lease,
                leaseId,
                documentType.Id,
                file.FileName,
                file.ContentType,
                memoryStream.ToArray()));
        }
    }
}

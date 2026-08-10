using KiraTakip.Authorization;
using KiraTakip.Extensions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Authorize(Policy = PermissionCatalog.TenantPortal.Lease.Module)]
[Route("Tenant/Leases")]
public class TenantLeaseController(
    ICurrentUserContext currentUserContext,
    ILeaseService leaseService,
    IStatisticsService statisticsService,
    IChargeService chargeService,
    IDocumentService documentService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tenantId = currentUserContext.TenantId!.Value;
        var leases = await leaseService.GetTenantPortalLeasesAsync(
            new GetTenantPortalLeasesInput(tenantId, BuildAccessScope()));

        return View(leases);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var tenantId = currentUserContext.TenantId!.Value;
        var now = DateTime.Now;
        var leaseDetails = await leaseService.GetTenantDetailsAsync(
            new GetTenantLeaseDetailsInput(id, tenantId, BuildAccessScope()));
        var summary = await statisticsService.GetLeaseSummaryAsync(
            new GetLeaseSummaryInput(
                leaseDetails.Id,
                leaseDetails.TenantId,
                leaseDetails.UnitId,
                leaseDetails.UnitArea,
                leaseDetails.StartDate,
                leaseDetails.EndDate,
                leaseDetails.Status,
                now));
        var hasChargeAccess = User.HasModuleAccess(
            PermissionCatalog.TenantPortal.Charge.Module);
        var chargeData = await chargeService.GetTenantLeaseDataAsync(
            new GetTenantLeaseChargeDataInput(
                tenantId,
                id,
                now.Date,
                hasChargeAccess));

        var viewModel = new TenantLeaseDetailsViewModel
        {
            Lease = leaseDetails,
            RemainingDays = summary.RemainingDays,
            MonthlyAmount = summary.MonthlyAmount,
            AnnualAmount = summary.AnnualAmount,
            IsActive = summary.IsActive,
            DurationPercentage = summary.DurationPercentage,
            UnitStatus = summary.UnitStatus,
            Charges = chargeData.Charges,
            HasChargeAccess = hasChargeAccess,
            CurrentLineItems = chargeData.CurrentCharge.LineItems,
            CurrentLineItemPeriod = chargeData.CurrentCharge.Period,
            EffectiveVatRate = leaseDetails.LeaseRateOverrides
                .FirstOrDefault(rate => rate.ChargeTypeBehavior == ChargeTypeBehavior.MonthlyFixed)?.VatRate ?? 20m
        };

        var deposits = await leaseService.GetDepositsAsync(
            new GetLeaseDepositsInput([id], tenantId));
        viewModel.DepositAmount = deposits.TryGetValue(id, out var deposit) ? deposit : null;

        viewModel.DocumentTypes = await documentService.GetTypesAsync(
            new GetDocumentTypesInput(DocumentOwnerType.Lease));
        viewModel.Documents = await documentService.GetListAsync(
            new GetDocumentsInput(
                DocumentOwnerType.Lease,
                id,
                new DocumentAccessScopeInput(
                    [DocumentOwnerType.Lease],
                    TenantId: tenantId)));

        return View(viewModel);
    }

    private LeaseAccessScopeInput BuildAccessScope()
        => permissionScopeProvider.GlobalAccess
            ? new LeaseAccessScopeInput()
            : new LeaseAccessScopeInput(
                permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.AccessibleUnitIds);}

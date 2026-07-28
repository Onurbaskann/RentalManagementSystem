using System.Globalization;
using KiraTakip.Authorization;
using KiraTakip.Extensions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Controllers;

[Authorize(Policy = "TenantUser")]
[RequireKiraciId]
[Route("Tenant/Panel")]
public class TenantPanelController(
    ICurrentUserContext currentUserContext,
    ITenantPanelService tenantPanelService,
    IPermissionScopeProvider permissionScopeProvider) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var tenantId = currentUserContext.TenantId!.Value;
        var today = DateTime.Today;
        var canViewLeases = User.HasModuleAccess(PermissionCatalog.TenantPortal.Lease.Module);
        var canViewCharges = User.HasModuleAccess(PermissionCatalog.TenantPortal.Charge.Module);
        var canViewPayments = User.HasModuleAccess(PermissionCatalog.TenantPortal.Payment.Module);
        var canViewReservations = User.HasModuleAccess(
            PermissionCatalog.TenantPortal.Reservation.Module);
        var dashboard = await tenantPanelService.GetDashboardAsync(
            new GetTenantPanelDashboardInput(
                tenantId,
                currentUserContext.UserId ?? string.Empty,
                today,
                canViewLeases,
                canViewCharges,
                canViewPayments,
                permissionScopeProvider.GlobalAccess ? null : permissionScopeProvider.AccessiblePropertyIds,
                permissionScopeProvider.GlobalAccess ? null : permissionScopeProvider.AccessibleUnitIds));

        var userRole = User.HasPermission(PermissionCatalog.TenantPortal.System.User.Invite)
            ? "Firma Yetkilisi"
            : canViewPayments
                ? "Finans Yetkilisi"
                : "Kiracı";

        var viewModel = new TenantPanelViewModel
        {
            TenantName = dashboard.TenantName,
            UserName = dashboard.UserName,
            UserRole = userRole,
            CanViewLeases = canViewLeases,
            CanViewCharges = canViewCharges,
            CanViewPayments = canViewPayments,
            CanViewReservations = canViewReservations,
            DateLabel = today.ToString(
                "d MMMM yyyy, dddd",
                CultureInfo.GetCultureInfo("tr-TR")),
            ActiveLeaseCount = dashboard.ActiveLeaseCount,
            TotalOutstandingDebt = dashboard.TotalOutstandingDebt,
            UpcomingPaymentCount = dashboard.UpcomingPaymentCount,
            UpcomingPaymentAmount = dashboard.UpcomingPaymentAmount,
            OverdueCount = dashboard.OverdueCount,
            OverdueAmount = dashboard.OverdueAmount,
            MonthlyCashFlow = dashboard.MonthlyCashFlow.Select(item => new TenantPanelMonthlyCashFlow
            {
                MonthLabel = item.MonthLabel,
                Expected = item.Expected,
                Paid = item.Paid
            }).ToList(),
            DebtTypeDistribution = dashboard.DebtTypeDistribution.Select(item => new TenantPanelDebtSlice
            {
                Name = item.Name,
                Amount = item.Amount
            }).ToList(),
            DebtBalanceSparkline = dashboard.DebtBalanceSparkline,
            UpcomingCharges = dashboard.UpcomingCharges.Select(item => new TenantPanelUpcomingChargeItem
            {
                ChargeId = item.ChargeId,
                Period = item.Period,
                UnitName = item.UnitName,
                DueDate = item.DueDate,
                DayDifference = item.DayDifference,
                RemainingAmount = item.RemainingAmount,
                BorderColor = item.BorderColor
            }).ToList(),
            RecentPayments = dashboard.RecentPayments.Select(item => new TenantPanelRecentPaymentItem
            {
                PaymentId = item.PaymentId,
                PaymentDate = item.PaymentDate,
                Amount = item.Amount,
                ChannelName = item.ChannelName,
                StatusName = item.StatusName,
                StatusDotColor = item.StatusDotColor
            }).ToList()
        };

        return View(viewModel);
    }
}

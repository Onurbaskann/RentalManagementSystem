using KiraTakip.Authorization;
using KiraTakip.Extensions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;

namespace KiraTakip.Controllers;

[Authorize]
public class HomeController(
    IPropertyService propertyService,
    ILeaseService leaseService,
    IChargeService chargeService,
    IPaymentService paymentService,
    IBankTransactionService bankTransactionService,
    IReservationService reservationService,
    UserManager<ApplicationUser> userManager,
    IPermissionScopeCache permissionScopeCache) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var currentUser = user!;

        if (currentUser.UserType == UserType.Tenant)
            return RedirectToAction(nameof(TenantPanelController.Index), "TenantPanel");

        var now = DateTime.Now;
        var today = DateTime.Today;
        var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
        var scope = await permissionScopeCache.GetAsync(currentUser.Id);
        var propertyIds = scope.GlobalAccess ? null : scope.PropertyIds;
        var unitIds = scope.GlobalAccess ? null : scope.UnitIds;

        var properties = await propertyService.GetAllAsync(
            new GetPropertiesInput(propertyIds, unitIds));
        var leases = await leaseService.GetAllAsync(
            new GetLeasesInput(PropertyIds: propertyIds, UnitIds: unitIds));
        var availableUnits = await propertyService.GetAvailableUnitsAsync(
            new GetAvailableUnitsInput(propertyIds, unitIds));

        var activeLeases = leases.Where(lease => lease.IsActive).ToList();
        decimal totalMonthlyRevenue = 0m;
        foreach (var lease in activeLeases)
            totalMonthlyRevenue += lease.MonthlyAmount;

        var role = currentUser.IsSuperAdmin ? RoleNames.SistemYoneticisi
            : User.Claims.FirstOrDefault(claim => claim.Type == System.Security.Claims.ClaimTypes.Role)?.Value
            ?? "Kullanıcı";

        var viewModel = new DashboardViewModel
        {
            UserName = currentUser.AdSoyad ?? currentUser.Email ?? "Kullanıcı",
            UserRole = role,
            DateLabel = today.ToString("d MMMM yyyy, dddd", turkishCulture),
            TotalProperties = properties.Count,
            PropertyTypeDistribution = properties
                .GroupBy(property => string.IsNullOrEmpty(property.PropertyTypeName) ? "Diğer" : property.PropertyTypeName)
                .ToDictionary(group => group.Key, group => group.Count()),
            TotalUnits = properties.Sum(property => property.UnitCount),
            RentedUnits = properties.Sum(property => property.LeasedUnitCount),
            VacantUnits = properties.Sum(property => property.VacantUnitCount),
            ExpiringLeaseUnits = properties.Sum(property => property.ExpiringSoonUnitCount),
            ActiveLeases = activeLeases.Count,
            ActiveTenantCount = activeLeases.Select(lease => lease.TenantId).Distinct().Count(),
            TotalMonthlyRevenue = totalMonthlyRevenue,
            ProjectedAnnualRevenue = totalMonthlyRevenue * 12,
        };

        viewModel.RenewalsThisMonth = leases
            .Count(lease => lease.IsActive && lease.EndDate.Year == now.Year && lease.EndDate.Month == now.Month);

        viewModel.ExpiringLeases = leases
            .Where(lease => lease.IsActive && lease.RemainingDays <= 60)
            .OrderBy(lease => lease.EndDate)
            .Take(5)
            .Select(lease => new ExpiringLeaseSummary
            {
                LeaseId = lease.Id,
                TenantName = lease.TenantDisplayName,
                PropertyName = lease.PropertyName,
                UnitName = lease.UnitName,
                RemainingDays = lease.RemainingDays,
                EndDate = lease.EndDate
            }).ToList();

        viewModel.VacantUnitSummaries = availableUnits
            .Take(5)
            .Select(unit => new VacantUnitSummary
            {
                UnitId = unit.Id,
                PropertyName = unit.PropertyName,
                UnitName = unit.Name,
                District = unit.District,
                Area = unit.Area
            }).ToList();

        if (User.HasModuleAccess(PermissionCatalog.Payment.Module))
        {
            viewModel.HasPaymentAccess = true;
            await chargeService.UpdateDelaysAsync();
            var charges = await chargeService.GetListAsync(new GetChargesInput(
                PropertyIds: propertyIds,
                UnitIds: unitIds));
            var chargesThisMonth = charges
                .Where(charge => charge.PeriodStart.Year == now.Year && charge.PeriodStart.Month == now.Month)
                .ToList();

            viewModel.ExpectedCollectionThisMonth = chargesThisMonth.Sum(charge => charge.TotalAmount);
            viewModel.CollectedThisMonth = chargesThisMonth.Sum(charge => charge.PaidAmount);
            viewModel.OverdueChargeCount = charges.Count(charge => charge.Status == ChargeStatus.Overdue);
            viewModel.TotalOverdueAmount = charges
                .Where(charge => charge.Status == ChargeStatus.Overdue)
                .Sum(charge => charge.TotalAmount - charge.PaidAmount);

            var payments = await paymentService.GetAllAsync(new GetPaymentsInput(
                PropertyIds: propertyIds,
                UnitIds: unitIds));
            viewModel.PendingPaymentApprovalCount = payments.Count(payment => payment.Status == PaymentStatus.PendingApproval);

            var unmatchedTransactions = await bankTransactionService.GetAllAsync(
                new GetBankTransactionsInput(BankMatchStatus.Unmatched));
            viewModel.UnmatchedBankTransactionCount = unmatchedTransactions.Count;

            viewModel.ManualChargeTotalThisMonth = chargesThisMonth
                .Where(charge => charge.SourceType == ChargeSourceType.Manual && charge.Status != ChargeStatus.Cancelled)
                .Sum(charge => charge.TotalAmount);
            viewModel.ReservationRevenueThisMonth = chargesThisMonth
                .Where(charge => charge.SourceType == ChargeSourceType.Reservation && charge.Status != ChargeStatus.Cancelled)
                .Sum(charge => charge.TotalAmount);

            var reservations = await reservationService.GetAllAsync(
                new GetReservationsInput(propertyIds, unitIds));
            viewModel.UntransferredReservationCount = reservations
                .Count(reservation => reservation.Status == ReservationStatus.Planned
                    && reservation.TotalAmount > 0
                    && reservation.ChargeId == null);

            // --- Redesign metrikleri ---
            // Son 6 ay nakit akışı + tahsilat oranı sparkline
            var sixMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
            var monthlyGroups = charges
                .Where(charge => charge.PeriodStart >= sixMonthStart && charge.Status != ChargeStatus.Cancelled)
                .GroupBy(charge => new { charge.PeriodStart.Year, charge.PeriodStart.Month })
                .ToDictionary(
                    group => (group.Key.Year, group.Key.Month),
                    group => (
                        Expected: group.Sum(charge => charge.TotalAmount),
                        Collected: group.Sum(charge => charge.PaidAmount)));

            for (var monthOffset = 5; monthOffset >= 0; monthOffset--)
            {
                var month = new DateTime(today.Year, today.Month, 1).AddMonths(-monthOffset);
                var bucket = monthlyGroups.TryGetValue((month.Year, month.Month), out var monthlyValues)
                    ? monthlyValues
                    : (Expected: 0m, Collected: 0m);

                viewModel.MonthlyCashFlow.Add(new DashboardMonthlyCashFlow
                {
                    MonthLabel = turkishCulture.DateTimeFormat.GetAbbreviatedMonthName(month.Month),
                    Expected = bucket.Expected,
                    Collected = bucket.Collected
                });

                var collectionRate = bucket.Expected > 0
                    ? (double)(bucket.Collected / bucket.Expected) * 100
                    : 0;
                viewModel.CollectionRateSparkline.Add(Math.Round(collectionRate, 1));
            }

            // Tahsilat oranı — son 30 gün vade dolan tahakkuklar
            var thirtyDaysAgo = today.AddDays(-30);
            var lastThirtyDays = charges
                .Where(charge => charge.DueDate >= thirtyDaysAgo
                    && charge.DueDate <= today
                    && charge.Status != ChargeStatus.Cancelled)
                .ToList();
            var expectedLastThirtyDays = lastThirtyDays.Sum(charge => charge.TotalAmount);
            var collectedLastThirtyDays = lastThirtyDays.Sum(charge => charge.PaidAmount);
            viewModel.ThirtyDayCollectionRate = expectedLastThirtyDays > 0
                ? Math.Round(collectedLastThirtyDays / expectedLastThirtyDays * 100m, 1)
                : 0m;

            // Momentum — bu ay vs geçen ay (beklenen tahsilat üzerinden)
            var previousMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            var previousMonthEnd = previousMonthStart.AddMonths(1).AddDays(-1);
            viewModel.MonthlyRevenueLastMonth = charges
                .Where(charge => charge.PeriodStart >= previousMonthStart
                    && charge.PeriodStart <= previousMonthEnd
                    && charge.Status != ChargeStatus.Cancelled)
                .Sum(charge => charge.TotalAmount);
            viewModel.MonthlyRevenueChange = viewModel.MonthlyRevenueLastMonth > 0
                ? Math.Round(
                    (viewModel.ExpectedCollectionThisMonth - viewModel.MonthlyRevenueLastMonth)
                    / viewModel.MonthlyRevenueLastMonth * 100m,
                    1)
                : 0m;

            // Bugün vade dolan
            var chargesDueToday = charges.Where(charge => charge.DueDate.Date == today
                && (charge.Status == ChargeStatus.Pending
                    || charge.Status == ChargeStatus.PartiallyPaid
                    || charge.Status == ChargeStatus.Overdue))
                .ToList();
            viewModel.ChargesDueTodayCount = chargesDueToday.Count;
            viewModel.ChargesDueTodayAmount = chargesDueToday.Sum(charge => charge.TotalAmount - charge.PaidAmount);

            // Top 5 gelir getiren taşınmaz (son 12 ay charge dönemleri, ödenen tutara göre)
            var lastYear = today.AddYears(-1);
            var unitCountsByProperty = properties.ToDictionary(property => property.Id, property => property.UnitCount);
            viewModel.TopRevenueProperties = charges
                .Where(charge => charge.PeriodStart >= lastYear && charge.PropertyId != null && charge.PaidAmount > 0)
                .GroupBy(charge => new
                {
                    PropertyId = charge.PropertyId!.Value,
                    PropertyName = charge.PropertyName ?? "—"
                })
                .Select(group => new DashboardPropertyRevenue
                {
                    PropertyId = group.Key.PropertyId,
                    PropertyName = group.Key.PropertyName,
                    TotalCollected = group.Sum(charge => charge.PaidAmount),
                    UnitCount = unitCountsByProperty.TryGetValue(group.Key.PropertyId, out var unitCount) ? unitCount : 0
                })
                .OrderByDescending(property => property.TotalCollected)
                .Take(5)
                .ToList();

            viewModel.TopRevenueTenants = charges
                .Where(charge => charge.PeriodStart >= lastYear && charge.PaidAmount > 0)
                .GroupBy(charge => new
                {
                    TenantId = charge.TenantId,
                    TenantName = charge.TenantDisplayName ?? "—"
                })
                .Select(group => new DashboardTenantRevenue
                {
                    TenantId = group.Key.TenantId,
                    TenantName = group.Key.TenantName,
                    TotalCollected = group.Sum(charge => charge.PaidAmount),
                    LeaseCount = group.Select(charge => charge.LeaseId).Distinct().Count()
                })
                .OrderByDescending(tenant => tenant.TotalCollected)
                .Take(5)
                .ToList();
        }

        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

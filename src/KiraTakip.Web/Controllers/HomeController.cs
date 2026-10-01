using KiraTakip.Authorization;
using KiraTakip.Web.Extensions;
using KiraTakip.Web.Authorization;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Services.Interfaces.Banking;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Leases;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Properties;
using KiraTakip.Services.Interfaces.Reservations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Charge;
using KiraTakip.Models.Dtos.Payment;
using KiraTakip.Models.Dtos.BankTransaction;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Web.Controllers;

[Authorize]
public class HomeController(
    IPropertyService propertyService,
    ILeaseService leaseService,
    IChargeService chargeService,
    IPaymentService paymentService,
    IBankTransactionService bankTransactionService,
    IReservationService reservationService,
    IOperationalPolicyProvider operationalPolicyProvider,
    UserManager<ApplicationUser> userManager,
    IPermissionScopeCache permissionScopeCache,
    ICurrentUserPermissionService permissionService) : Controller
{
    private const string SuperAdminDisplayName = "Sistem Yöneticisi";

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

        var role = currentUser.IsSuperAdmin ? SuperAdminDisplayName
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

        // Doluluk Dağılımı — taşınmaz bazlı kırılım + adet/m² karşılaştırması
        viewModel.PropertyOccupancies = properties
            .Select(property => new DashboardPropertyOccupancy
            {
                PropertyId = property.Id,
                PropertyName = property.Name,
                UnitCount = property.UnitCount,
                LeasedUnitCount = property.LeasedUnitCount,
                ExpiringSoonUnitCount = property.ExpiringSoonUnitCount,
                VacantUnitCount = property.VacantUnitCount
            })
            .OrderByDescending(property => property.UnitCount)
            .ToList();

        var occupiedUnitCount = viewModel.RentedUnits + viewModel.ExpiringLeaseUnits;
        viewModel.OccupancyCountRate = viewModel.TotalUnits > 0
            ? Math.Round((decimal)occupiedUnitCount / viewModel.TotalUnits * 100m, 1)
            : 0m;

        var totalLeasedArea = properties.Sum(property => property.LeasedUnitArea);
        var totalTrackedArea = totalLeasedArea + properties.Sum(property => property.VacantUnitArea);
        viewModel.OccupancyAreaRate = totalTrackedArea > 0
            ? Math.Round(totalLeasedArea / totalTrackedArea * 100m, 1)
            : 0m;

        // Süresi Dolmak Üzere — durum özeti + ortalama süre özeti
        var leaseStatusLabels = new Dictionary<LeaseStatus, string>
        {
            [LeaseStatus.Active] = "Aktif",
            [LeaseStatus.Draft] = "Taslak",
            [LeaseStatus.RevisionRequested] = "Revizyon İstenen",
            [LeaseStatus.Ended] = "Sona Erdi",
            [LeaseStatus.Terminated] = "Feshedildi"
        };
        viewModel.LeaseStatusDistribution = leases
            .GroupBy(lease => lease.Status)
            .ToDictionary(
                group => leaseStatusLabels.TryGetValue(group.Key, out var label) ? label : group.Key.ToString(),
                group => group.Count());
        viewModel.AverageLeaseDurationMonths = leases.Count > 0
            ? Math.Round(leases.Average(lease => (lease.EndDate - lease.StartDate).TotalDays / 30.44), 1)
            : 0;

        viewModel.RenewalsThisMonth = leases
            .Count(lease => lease.IsActive && lease.EndDate.Year == now.Year && lease.EndDate.Month == now.Month);

        viewModel.ExpiringLeases = leases
            .Where(lease => lease.IsActive
                && lease.RemainingDays <= operationalPolicyProvider.Current.DashboardExpiringLeaseLookaheadDays)
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

        if (await permissionService.HasModuleAccessAsync(PermissionCatalog.Payment.Module))
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
                .Count(reservation => reservation.Status == ReservationStatus.Confirmed
                    && reservation.TotalAmount > 0
                    && reservation.ChargeId == null);

            // --- Redesign metrikleri ---
            var sixMonthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-5);

            // Gelir Kırılımı pastası — son 6 ay, kalem tipine göre toplam tahsilat
            viewModel.ChargeTypeRevenueBreakdown = charges
                .Where(charge => charge.PeriodStart >= sixMonthStart && charge.Status != ChargeStatus.Cancelled)
                .SelectMany(charge => charge.LineItems)
                .GroupBy(lineItem => lineItem.ChargeTypeName)
                .Select(group => new DashboardChargeTypeTotal
                {
                    ChargeTypeName = group.Key,
                    TotalCollected = group.Sum(lineItem => lineItem.PaidAmount)
                })
                .OrderByDescending(item => item.TotalCollected)
                .ToList();

            // Aylık Kira Geliri kartı — kaynak (Tümü/Sözleşme/Manuel/Rezervasyon) × zaman aralığı
            // (3/6/12 ay) kombinasyonları; kalemlerin toplamı, KDV dahil, tahakkuk dönemine göre.
            List<DashboardMonthlyRevenueTrendRow> BuildTrendRows(IEnumerable<ChargeListItemDto> scopedCharges, int monthCount)
            {
                var windowStart = new DateTime(today.Year, today.Month, 1).AddMonths(-(monthCount - 1));
                var groups = scopedCharges
                    .Where(charge => charge.PeriodStart >= windowStart && charge.Status != ChargeStatus.Cancelled)
                    .GroupBy(charge => new { charge.PeriodStart.Year, charge.PeriodStart.Month })
                    .ToDictionary(
                        group => (group.Key.Year, group.Key.Month),
                        group => (
                            Expected: group.Sum(charge => charge.TotalAmount),
                            Collected: group.Sum(charge => charge.PaidAmount)));

                var rows = new List<DashboardMonthlyRevenueTrendRow>();
                for (var monthOffset = monthCount - 1; monthOffset >= 0; monthOffset--)
                {
                    var month = new DateTime(today.Year, today.Month, 1).AddMonths(-monthOffset);
                    var bucket = groups.TryGetValue((month.Year, month.Month), out var value)
                        ? value
                        : (Expected: 0m, Collected: 0m);
                    rows.Add(new DashboardMonthlyRevenueTrendRow
                    {
                        MonthLabel = turkishCulture.DateTimeFormat.GetAbbreviatedMonthName(month.Month),
                        Amount = bucket.Expected,
                        CollectedAmount = bucket.Collected,
                        CollectionRatePercent = bucket.Expected > 0
                            ? Math.Round(bucket.Collected / bucket.Expected * 100m, 1)
                            : null
                    });
                }

                rows.Reverse(); // en yeni ay üstte
                return rows;
            }

            DashboardRevenueTrendSet BuildTrendSet(IEnumerable<ChargeListItemDto> scopedCharges)
            {
                var scopedList = scopedCharges as List<ChargeListItemDto> ?? scopedCharges.ToList();
                return new DashboardRevenueTrendSet
                {
                    Months3 = BuildTrendRows(scopedList, 3),
                    Months6 = BuildTrendRows(scopedList, 6),
                    Months12 = BuildTrendRows(scopedList, 12)
                };
            }

            viewModel.RevenueTrendAll = BuildTrendSet(charges);
            viewModel.RevenueTrendLease = BuildTrendSet(charges.Where(charge => charge.SourceType == ChargeSourceType.Lease));
            viewModel.RevenueTrendManual = BuildTrendSet(charges.Where(charge => charge.SourceType == ChargeSourceType.Manual));
            viewModel.RevenueTrendReservation = BuildTrendSet(charges.Where(charge => charge.SourceType == ChargeSourceType.Reservation));

            // Kartın üst tutarı çizelgenin son (en güncel ay) noktasıyla birebir aynı olsun (varsayılan: Tümü + 6 Ay)
            var latestRevenueTrend = viewModel.RevenueTrendAll.Months6.FirstOrDefault();
            viewModel.TotalMonthlyRevenue = latestRevenueTrend?.Amount ?? 0m;
            viewModel.MonthlyRevenueCollectionRate = latestRevenueTrend?.CollectionRatePercent ?? 0m;

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

            viewModel.TopRevenueStores = payments
                .Where(payment => payment.PaymentDate >= lastYear && payment.Status == PaymentStatus.Approved)
                .GroupBy(payment => new { payment.StoreId, payment.StoreName })
                .Select(group => new DashboardStoreRevenue
                {
                    StoreId = group.Key.StoreId,
                    StoreName = group.Key.StoreName,
                    TotalCollected = group.Sum(payment => payment.Amount),
                    PaymentCount = group.Count()
                })
                .OrderByDescending(store => store.TotalCollected)
                .Take(5)
                .ToList();

            viewModel.RiskyTenants = charges
                .Where(charge => charge.DueDate < today && charge.TotalAmount > charge.PaidAmount && charge.Status != ChargeStatus.Cancelled)
                .GroupBy(charge => new { charge.TenantId, TenantName = charge.TenantDisplayName ?? "—" })
                .Select(group => new DashboardRiskyTenant
                {
                    TenantId = group.Key.TenantId,
                    TenantName = group.Key.TenantName,
                    OverdueAmount = group.Sum(charge => charge.TotalAmount - charge.PaidAmount),
                    OverdueChargeCount = group.Count()
                })
                .OrderByDescending(tenant => tenant.OverdueAmount)
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

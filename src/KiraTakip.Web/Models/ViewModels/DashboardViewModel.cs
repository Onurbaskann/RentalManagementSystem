namespace KiraTakip.Web.Models.ViewModels;

public class DashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;

    public int TotalRootSpaces { get; set; }
    public int RentableSpaces { get; set; }
    public int LeasedSpaces { get; set; }
    public int VacantSpaces { get; set; }
    public int ActiveLeases { get; set; }
    public int RenewalsThisMonth { get; set; }
    public decimal TotalMonthlyRevenue { get; set; }
    public decimal ProjectedAnnualRevenue { get; set; }
    public List<ExpiringLeaseSummary> ExpiringLeases { get; set; } = [];
    public List<VacantSpaceSummary> VacantSpaceSummaries { get; set; } = [];

    public bool HasPaymentAccess { get; set; }
    public decimal ExpectedCollectionThisMonth { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public int OverdueChargeCount { get; set; }
    public decimal TotalOverdueAmount { get; set; }
    public int PendingPaymentApprovalCount { get; set; }
    public int UnmatchedBankTransactionCount { get; set; }

    public decimal ManualChargeTotalThisMonth { get; set; }
    public decimal ReservationRevenueThisMonth { get; set; }
    public int UntransferredReservationCount { get; set; }

    public decimal ThirtyDayCollectionRate { get; set; }
    public decimal MonthlyRevenueCollectionRate { get; set; }
    public int ChargesDueTodayCount { get; set; }
    public decimal ChargesDueTodayAmount { get; set; }
    public List<DashboardRootSpaceRevenue> TopRevenueRootSpaces { get; set; } = [];
    public List<DashboardTenantRevenue> TopRevenueTenants { get; set; } = [];
    public List<DashboardStoreRevenue> TopRevenueStores { get; set; } = [];
    public List<DashboardRiskyTenant> RiskyTenants { get; set; } = [];
    public int ActiveTenantCount { get; set; }

    // Gelir Kırılımı — kalem tipine göre son 6 ay tahsilat toplamı
    public List<DashboardChargeTypeTotal> ChargeTypeRevenueBreakdown { get; set; } = [];

    // Aylık Kira Geliri — kaynak (Tümü/Sözleşme/Manuel/Rezervasyon) × zaman aralığı (3/6/12 ay) kombinasyonları
    public DashboardRevenueTrendSet RevenueTrendAll { get; set; } = new();
    public DashboardRevenueTrendSet RevenueTrendLease { get; set; } = new();
    public DashboardRevenueTrendSet RevenueTrendManual { get; set; } = new();
    public DashboardRevenueTrendSet RevenueTrendReservation { get; set; } = new();

    // Doluluk Dağılımı — kök alan bazlı: kiralanabilir alan / aktif sözleşmeli alan
    public List<DashboardSpaceOccupancy> SpaceOccupancies { get; set; } = [];
    public decimal OccupancyCountRate { get; set; }

    // Süresi Dolmak Üzere — durum özeti + süre özeti
    public Dictionary<string, int> LeaseStatusDistribution { get; set; } = [];
    public double AverageLeaseDurationMonths { get; set; }
}

public class DashboardRootSpaceRevenue
{
    public int RootSpaceId { get; set; }
    public string RootSpaceName { get; set; } = string.Empty;
    public decimal TotalCollected { get; set; }
    public int RentableSpaceCount { get; set; }
}

public class DashboardTenantRevenue
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public decimal TotalCollected { get; set; }
    public int LeaseCount { get; set; }
}

public class DashboardStoreRevenue
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal TotalCollected { get; set; }
    public int PaymentCount { get; set; }
}

public class DashboardRiskyTenant
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public decimal OverdueAmount { get; set; }
    public int OverdueChargeCount { get; set; }
}

public class DashboardChargeTypeTotal
{
    public string ChargeTypeName { get; set; } = string.Empty;
    public decimal TotalCollected { get; set; }
}

public class DashboardMonthlyRevenueTrendRow
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal? CollectionRatePercent { get; set; }
}

public class DashboardRevenueTrendSet
{
    public List<DashboardMonthlyRevenueTrendRow> Months3 { get; set; } = [];
    public List<DashboardMonthlyRevenueTrendRow> Months6 { get; set; } = [];
    public List<DashboardMonthlyRevenueTrendRow> Months12 { get; set; } = [];
}

public class DashboardSpaceOccupancy
{
    public int RootSpaceId { get; set; }
    public string RootSpaceName { get; set; } = string.Empty;
    public int RentableSpaceCount { get; set; }
    public int LeasedSpaceCount { get; set; }
}

public class ExpiringLeaseSummary
{
    public int LeaseId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string SpaceName { get; set; } = string.Empty;
    public int RemainingDays { get; set; }
    public DateTime EndDate { get; set; }
}

public class VacantSpaceSummary
{
    public int SpaceId { get; set; }
    public string RootSpaceName { get; set; } = string.Empty;
    public string SpaceName { get; set; } = string.Empty;
    public decimal Area { get; set; }
}

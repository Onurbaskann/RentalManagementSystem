namespace KiraTakip.Web.Models.ViewModels;

public class DashboardViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;

    public int TotalProperties { get; set; }
    public Dictionary<string, int> PropertyTypeDistribution { get; set; } = [];
    public int TotalUnits { get; set; }
    public int RentedUnits { get; set; }
    public int VacantUnits { get; set; }
    public int ExpiringLeaseUnits { get; set; }
    public int ActiveLeases { get; set; }
    public int RenewalsThisMonth { get; set; }
    public decimal TotalMonthlyRevenue { get; set; }
    public decimal ProjectedAnnualRevenue { get; set; }
    public List<ExpiringLeaseSummary> ExpiringLeases { get; set; } = [];
    public List<VacantUnitSummary> VacantUnitSummaries { get; set; } = [];

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
    public List<DashboardPropertyRevenue> TopRevenueProperties { get; set; } = [];
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

    // Doluluk Dağılımı — taşınmaz bazlı + m² karşılaştırması
    public List<DashboardPropertyOccupancy> PropertyOccupancies { get; set; } = [];
    public decimal OccupancyCountRate { get; set; }
    public decimal OccupancyAreaRate { get; set; }

    // Süresi Dolmak Üzere — durum özeti + süre özeti
    public Dictionary<string, int> LeaseStatusDistribution { get; set; } = [];
    public double AverageLeaseDurationMonths { get; set; }
}

public class DashboardPropertyRevenue
{
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public decimal TotalCollected { get; set; }
    public int UnitCount { get; set; }
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

public class DashboardPropertyOccupancy
{
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public int UnitCount { get; set; }
    public int LeasedUnitCount { get; set; }
    public int ExpiringSoonUnitCount { get; set; }
    public int VacantUnitCount { get; set; }
}

public class ExpiringLeaseSummary
{
    public int LeaseId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public int RemainingDays { get; set; }
    public DateTime EndDate { get; set; }
}

public class VacantUnitSummary
{
    public int UnitId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public decimal Area { get; set; }
}

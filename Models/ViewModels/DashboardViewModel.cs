namespace KiraTakip.Models.ViewModels;

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

    public List<DashboardMonthlyCashFlow> MonthlyCashFlow { get; set; } = [];
    public List<double> CollectionRateSparkline { get; set; } = [];
    public decimal ThirtyDayCollectionRate { get; set; }
    public decimal MonthlyRevenueLastMonth { get; set; }
    public decimal MonthlyRevenueChange { get; set; }
    public int ChargesDueTodayCount { get; set; }
    public decimal ChargesDueTodayAmount { get; set; }
    public List<DashboardPropertyRevenue> TopRevenueProperties { get; set; } = [];
    public List<DashboardTenantRevenue> TopRevenueTenants { get; set; } = [];
    public int ActiveTenantCount { get; set; }
}

public class DashboardMonthlyCashFlow
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Expected { get; set; }
    public decimal Collected { get; set; }
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

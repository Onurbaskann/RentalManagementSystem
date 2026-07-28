namespace KiraTakip.Models.ViewModels;

public class TenantPanelViewModel
{
    public string TenantName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;
    public bool CanViewLeases { get; set; }
    public bool CanViewCharges { get; set; }
    public bool CanViewPayments { get; set; }
    public bool CanViewReservations { get; set; }

    public int ActiveLeaseCount { get; set; }
    public decimal TotalOutstandingDebt { get; set; }
    public int UpcomingPaymentCount { get; set; }
    public decimal UpcomingPaymentAmount { get; set; }
    public int OverdueCount { get; set; }
    public decimal OverdueAmount { get; set; }

    public List<TenantPanelMonthlyCashFlow> MonthlyCashFlow { get; set; } = [];
    public List<TenantPanelDebtSlice> DebtTypeDistribution { get; set; } = [];
    public List<decimal> DebtBalanceSparkline { get; set; } = [];

    public List<TenantPanelUpcomingChargeItem> UpcomingCharges { get; set; } = [];
    public List<TenantPanelRecentPaymentItem> RecentPayments { get; set; } = [];
}

public class TenantPanelMonthlyCashFlow
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Expected { get; set; }
    public decimal Paid { get; set; }
}

public class TenantPanelDebtSlice
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TenantPanelUpcomingChargeItem
{
    public int ChargeId { get; set; }
    public string Period { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DayDifference { get; set; }
    public decimal RemainingAmount { get; set; }
    public string BorderColor { get; set; } = string.Empty;
}

public class TenantPanelRecentPaymentItem
{
    public int PaymentId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string StatusDotColor { get; set; } = string.Empty;
}

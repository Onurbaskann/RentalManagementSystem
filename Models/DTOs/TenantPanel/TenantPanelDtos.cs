namespace KiraTakip.Models.Dtos;

public record GetTenantPanelDashboardInput(
    int TenantId,
    string UserId,
    DateTime Today,
    bool CanViewLeases,
    bool CanViewCharges,
    bool CanViewPayments,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetTenantPanelChargeDataInput(
    int TenantId,
    DateTime Today,
    bool IncludeDebtData,
    bool IncludeMonthlyExpected,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetTenantPanelPaymentDataInput(
    int TenantId,
    DateTime Today,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record TenantPanelMonthlyTotalDto(int Year, int Month, decimal Total);

public record TenantPanelDebtSliceDto(string Name, decimal Amount);

public record TenantPanelUpcomingChargeDataDto(
    int ChargeId,
    DateTime PeriodStart,
    DateTime DueDate,
    decimal RemainingAmount,
    string? PropertyName,
    string? UnitName);

public record TenantPanelRecentPaymentDataDto(
    int PaymentId,
    DateTime PaymentDate,
    decimal Amount,
    PaymentChannel PaymentChannel,
    PaymentStatus Status);

public record TenantPanelChargeDataDto(
    decimal TotalOutstandingDebt,
    int UpcomingPaymentCount,
    decimal UpcomingPaymentAmount,
    int OverdueCount,
    decimal OverdueAmount,
    List<TenantPanelMonthlyTotalDto> MonthlyExpected,
    List<decimal> DebtBalanceSparkline,
    List<TenantPanelUpcomingChargeDataDto> UpcomingCharges);

public record TenantPanelPaymentDataDto(
    List<TenantPanelMonthlyTotalDto> MonthlyPaid,
    List<TenantPanelRecentPaymentDataDto> RecentPayments);

public record TenantPanelMonthlyCashFlowDto(
    string MonthLabel,
    decimal Expected,
    decimal Paid);

public record TenantPanelUpcomingChargeDto(
    int ChargeId,
    string Period,
    string UnitName,
    DateTime DueDate,
    int DayDifference,
    decimal RemainingAmount,
    string BorderColor);

public record TenantPanelRecentPaymentDto(
    int PaymentId,
    DateTime PaymentDate,
    decimal Amount,
    string ChannelName,
    string StatusName,
    string StatusDotColor);

public record TenantPanelDashboardDto(
    string TenantName,
    string UserName,
    int ActiveLeaseCount,
    decimal TotalOutstandingDebt,
    int UpcomingPaymentCount,
    decimal UpcomingPaymentAmount,
    int OverdueCount,
    decimal OverdueAmount,
    List<TenantPanelMonthlyCashFlowDto> MonthlyCashFlow,
    List<TenantPanelDebtSliceDto> DebtTypeDistribution,
    List<decimal> DebtBalanceSparkline,
    List<TenantPanelUpcomingChargeDto> UpcomingCharges,
    List<TenantPanelRecentPaymentDto> RecentPayments);

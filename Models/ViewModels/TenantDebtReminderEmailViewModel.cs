namespace KiraTakip.Models.ViewModels;

public class TenantDebtReminderEmailViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName => string.IsNullOrWhiteSpace(LastName)
        ? FirstName
        : $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public List<DebtReminderLineViewModel> Debts { get; set; } = [];
    public string PaymentLink { get; set; } = string.Empty;
    public string PaymentLinkValidityText { get; set; } = string.Empty;
}

public class DebtReminderLineViewModel
{
    public string PropertyName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => TotalAmount - PaidAmount;
}

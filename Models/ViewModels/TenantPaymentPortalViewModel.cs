namespace KiraTakip.Models.ViewModels;

public class TenantPaymentPortalViewModel
{
    public string TenantName { get; set; } = string.Empty;
    public List<PaymentPortalChargeCardViewModel> ChargeCards { get; set; } = [];
    public int DefaultSelectedId { get; set; }
}

public class PaymentPortalChargeCardViewModel
{
    public int ChargeId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => TotalAmount - PaidAmount;
}

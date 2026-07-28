namespace KiraTakip.Models.ViewModels;

public class RejectPaymentViewModel
{
    public int PaymentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class CreatePaymentViewModel
{
    public int ChargeId { get; set; }
    public int? ChargeLineItemId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    public decimal Amount { get; set; }
    public PaymentChannel PaymentChannel { get; set; } = PaymentChannel.Eft;
    public string? Description { get; set; }
    public ChargeDetailDto? Charge { get; set; }
}

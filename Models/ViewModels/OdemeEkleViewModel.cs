using System.ComponentModel.DataAnnotations;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class OdemeEkleViewModel
{
    public int ChargeId { get; set; }
    public int? LeaseId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount sıfırdan büyük olmalıdır.")]
    public decimal Amount { get; set; }
    public PaymentChannel PaymentChannel { get; set; } = PaymentChannel.Eft;
    public string? Aciklama { get; set; }
    public TahakkukDetayDto? Charge { get; set; }
}

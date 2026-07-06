namespace KiraTakip.Models.Dtos;

public class TahakkukOdemeDto
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentChannel PaymentChannel { get; set; }
    public PaymentStatus Durum { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Aciklama { get; set; }
    public string? RejectionReason { get; set; }
}

namespace KiraTakip.Models.Dtos;

public class OdemeAdayDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentStatus Durum { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
}

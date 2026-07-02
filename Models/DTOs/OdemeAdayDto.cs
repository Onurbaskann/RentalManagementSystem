namespace KiraTakip.Models.Dtos;

public class OdemeAdayDto
{
    public int Id { get; set; }
    public decimal Tutar { get; set; }
    public DateTime OdemeTarihi { get; set; }
    public PaymentStatus Durum { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime DonemBaslangic { get; set; }
}

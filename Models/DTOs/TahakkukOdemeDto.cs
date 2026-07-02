namespace KiraTakip.Models.Dtos;

public class TahakkukOdemeDto
{
    public int Id { get; set; }
    public DateTime OdemeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public PaymentChannel PaymentChannel { get; set; }
    public PaymentStatus Durum { get; set; }
    public DateTime GirisTarihi { get; set; }
    public string? Aciklama { get; set; }
    public string? RedNedeni { get; set; }
}

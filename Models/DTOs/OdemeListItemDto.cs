namespace KiraTakip.Models.Dtos;

public class OdemeListItemDto
{
    public int Id { get; set; }
    public int TahakkukId { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public DateTime OdemeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public PaymentChannel PaymentChannel { get; set; }
    public PaymentSourceType PaymentSourceType { get; set; }
    public PaymentStatus Durum { get; set; }
    public DateTime GirisTarihi { get; set; }
    public string? Aciklama { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime TahakkukDonemBaslangic { get; set; }
    public string? GirenUserGosterimAdi { get; set; }
}

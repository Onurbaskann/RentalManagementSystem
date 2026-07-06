namespace KiraTakip.Models.Dtos;

public class OdemeListItemDto
{
    public int Id { get; set; }
    public int ChargeId { get; set; }
    public int? LeaseId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentChannel PaymentChannel { get; set; }
    public PaymentSourceType PaymentSourceType { get; set; }
    public PaymentStatus Durum { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Aciklama { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime TahakkukDonemBaslangic { get; set; }
    public string? GirenUserGosterimAdi { get; set; }
}

namespace KiraTakip.Models.Dtos;

public class ManuelBorcListItemDto
{
    public int Id { get; set; }
    public int? LeaseId { get; set; }
    public string? KiraciGosterimAdi { get; set; }
    public string? TasinmazAd { get; set; }
    public string? BirimAd { get; set; }
    public string? ChargeTypeCode { get; set; }
    public string? IlkKalemAciklama { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; }
    public ChargeStatus Durum { get; set; }
    public string? CancellationNote { get; set; }
    public int KiraciId { get; set; }
    public string? KiraciKategoriAd { get; set; }
}

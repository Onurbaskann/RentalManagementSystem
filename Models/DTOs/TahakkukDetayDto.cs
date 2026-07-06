namespace KiraTakip.Models.Dtos;

public class TahakkukDetayDto
{
    public int Id { get; set; }
    public int? LeaseId { get; set; }
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public int? TasinmazId { get; set; }
    public string? TasinmazAd { get; set; }
    public int? BirimId { get; set; }
    public string? BirimAd { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime DueDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal PaidAmount { get; set; }
    public ChargeStatus Durum { get; set; }
    public ChargeSourceType SourceType { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public List<TahakkukKalemDto> LineItems { get; set; } = [];
    public List<TahakkukOdemeDto> Allocations { get; set; } = [];
}
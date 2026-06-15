namespace KiraTakip.Models.Dtos;

public class ManuelBorcListItemDto
{
    public int Id { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public string? KiraciGosterimAdi { get; set; }
    public string? TasinmazAd { get; set; }
    public string? BirimAd { get; set; }
    public string? BorcTipiKod { get; set; }
    public string? IlkKalemAciklama { get; set; }
    public decimal BeklenenTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public DateTime VadeTarihi { get; set; }
    public TahakkukDurumu Durum { get; set; }
    public string? IptalNotu { get; set; }
    public int? KiraciId { get; set; }
    public KiraciTuru? KiraciTuru { get; set; }
    public string? KiraciKategoriAd { get; set; }
}

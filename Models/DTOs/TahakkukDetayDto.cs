namespace KiraTakip.Models.Dtos;

public class TahakkukDetayDto
{
    public int Id { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public int? TasinmazId { get; set; }
    public string? TasinmazAd { get; set; }
    public int? BirimId { get; set; }
    public string? BirimAd { get; set; }
    public DateTime DonemBaslangic { get; set; }
    public DateTime DonemBitis { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal BeklenenTutar { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public TahakkukDurumu Durum { get; set; }
    public TahakkukKaynakTipi KaynakTipi { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public List<TahakkukKalemDto> Kalemler { get; set; } = [];
    public List<TahakkukOdemeDto> Odemeler { get; set; } = [];
}
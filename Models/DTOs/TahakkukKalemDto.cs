namespace KiraTakip.Models.Dtos;

public class TahakkukKalemDto
{
    public string BorcTipiKod { get; set; } = string.Empty;
    public int BorcTipiSira { get; set; }
    public string BorcTipiAd { get; set; } = string.Empty;
    public string Aciklama { get; set; } = string.Empty;
    public CalculationMethod CalculationMethod { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal Carpan { get; set; }
    public decimal Tutar { get; set; }
    public decimal KdvOrani { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public LineItemSourceType KaynakTipi { get; set; }
}

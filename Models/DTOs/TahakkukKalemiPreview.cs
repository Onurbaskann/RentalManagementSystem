namespace KiraTakip.Models.DTOs;

public class TahakkukKalemiPreview
{
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = string.Empty;
    public string BorcTipiKod { get; set; } = string.Empty;
    public BorcTipiDavranisi Davranis { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal Carpan { get; set; }
    public decimal Tutar { get; set; }
    public decimal KdvOrani { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public KalemKaynakTipi KaynakTipi { get; set; }
    public bool RateBulundu { get; set; }
    public string? Aciklama { get; set; }
}

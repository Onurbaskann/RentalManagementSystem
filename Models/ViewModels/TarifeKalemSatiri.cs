namespace KiraTakip.Models.ViewModels;

public class TarifeKalemSatiri
{
    public int KalemId { get; set; }
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = string.Empty;
    public string BorcTipiKod { get; set; } = string.Empty;
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}

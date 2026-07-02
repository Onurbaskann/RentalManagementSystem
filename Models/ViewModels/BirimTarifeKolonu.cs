namespace KiraTakip.Models.ViewModels;

public class BirimTarifeKolonu
{
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = string.Empty;
    public string BorcTipiKod { get; set; } = string.Empty;
    public ChargeTypeBehavior ChargeTypeBehavior { get; set; }
}

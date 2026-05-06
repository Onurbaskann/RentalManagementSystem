namespace KiraTakip.Models.ViewModels;

public class BirimOzelFiyatViewModel
{
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = "";
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = "";
    public List<BirimRateSatiri> Kalemler { get; set; } = [];
}

public class BirimRateSatiri
{
    public int RateId { get; set; }
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = "";
    public string BorcTipiKod { get; set; } = "";
    public bool OzelFiyatAktif { get; set; }
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}

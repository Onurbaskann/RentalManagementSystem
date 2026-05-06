namespace KiraTakip.Models.ViewModels;

public class TarifeDetayViewModel
{
    public int TarifeId { get; set; }
    public int Yil { get; set; }
    public string? Aciklama { get; set; }
    public bool Aktif { get; set; }
    public List<TarifeKalemSatiri> Kalemler { get; set; } = [];
}

public class TarifeKalemSatiri
{
    public int KalemId { get; set; }
    public int BorcTipiId { get; set; }
    public string BorcTipiAd { get; set; } = "";
    public string BorcTipiKod { get; set; } = "";
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}

public class TarifeYilEkleViewModel
{
    public int Yil { get; set; } = DateTime.Now.Year;
    public string? Aciklama { get; set; }
    public int? KopyalaYilId { get; set; }
}

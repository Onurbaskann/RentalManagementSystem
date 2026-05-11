namespace KiraTakip.Models.ViewModels;

public enum TarifeHiyerarsiKatmani
{
    Tasinmaz = 1,
    Birim    = 2,
    Sozlesme = 3
}

public class ParentTarifeKartViewModel
{
    public string KaynakAdi { get; set; } = "";
    public string? Aciklama { get; set; }
    public List<ParentTarifeSatir> Satirlar { get; set; } = [];
}

public class ParentTarifeSatir
{
    public string KategoriAd { get; set; } = "";
    public string BorcTipiAd { get; set; } = "";
    public HesaplamaYontemi HesaplamaYontemi { get; set; }
    public decimal BirimDeger { get; set; }
    public decimal KdvOrani { get; set; }
}

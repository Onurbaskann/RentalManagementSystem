namespace KiraTakip.Models.ViewModels;

public class ParentRezervasyonTarifeKartViewModel
{
    public string KaynakAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public List<ParentRezervasyonTarifeSatir> Satirlar { get; set; } = [];
}

public class ParentRezervasyonTarifeSatir
{
    public string UnitTypeAd { get; set; } = string.Empty;
    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; }
}

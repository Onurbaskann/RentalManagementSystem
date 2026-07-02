namespace KiraTakip.Models.ViewModels;

public class TarifeMatrisRezervasyonSatir
{
    public int RezervasyonTarifeId { get; set; }
    public int UnitTypeId { get; set; }
    public string UnitTypeAd { get; set; } = string.Empty;
    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; }
}

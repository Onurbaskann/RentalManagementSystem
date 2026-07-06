namespace KiraTakip.Models.ViewModels;

public enum TarifeHiyerarsiKatmani
{
    Property = 1,
    Unit    = 2,
    Lease = 3
}

public class ParentTarifeKartViewModel
{
    public string KaynakAdi { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public List<ParentTarifeSatir> Satirlar { get; set; } = [];
}

public class ParentTarifeSatir
{
    public string KategoriAd { get; set; } = string.Empty;
    public string ChargeTypeName { get; set; } = string.Empty;
    public CalculationMethod CalculationMethod { get; set; }
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
    public string? Kaynak { get; set; }
}

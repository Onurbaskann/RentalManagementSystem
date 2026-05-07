namespace KiraTakip.Models.ViewModels;

public class RezervasyonHesapSonucu
{
    public int ToplamSureDakika { get; set; }
    public int UcretsizSureDakika { get; set; }
    public int UcretliSureDakika { get; set; }
    public int UcretliPeriyotSayisi { get; set; }
    public decimal BirimUcret { get; set; }
    public decimal UcretTutar { get; set; }
    public decimal KdvOrani { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public bool KuralBulundu { get; set; }
    public string? HataMessaji { get; set; }
}

namespace KiraTakip.Models.ViewModels;

public class RezervasyonHesapSonucu
{
    public int TotalDurationMinutes { get; set; }
    public int FreeDurationMinutes { get; set; }
    public int PaidDurationMinutes { get; set; }
    public int UcretliPeriyotSayisi { get; set; }
    public decimal UnitRate { get; set; }
    public decimal RateAmount { get; set; }
    public decimal KdvRate { get; set; }
    public decimal KdvTutari { get; set; }
    public decimal ToplamTutar { get; set; }
    public bool KuralBulundu { get; set; }
    public string? HataMessaji { get; set; }
}

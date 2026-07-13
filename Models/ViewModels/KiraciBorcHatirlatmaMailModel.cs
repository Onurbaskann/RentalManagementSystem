namespace KiraTakip.Models.ViewModels;

public class KiraciBorcHatirlatmaMailModel
{
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string GosterimAdi => string.IsNullOrWhiteSpace(Soyad) ? Ad : $"{Ad} {Soyad}";
    public string Email { get; set; } = string.Empty;
    public List<BorcSatiri> Borclar { get; set; } = [];
    public string OdemeLink { get; set; } = string.Empty;
}

public class BorcSatiri
{
    public string PropertyName { get; set; } = string.Empty;
    public string BirimAdi { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime DueDate { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal KalanTutar => ToplamTutar - PaidAmount;
}

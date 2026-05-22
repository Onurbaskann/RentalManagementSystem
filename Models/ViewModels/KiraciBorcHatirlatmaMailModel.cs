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
    public string TasinmazAdi { get; set; } = string.Empty;
    public string BirimAdi { get; set; } = string.Empty;
    public DateTime DonemBaslangic { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar => ToplamTutar - OdenenTutar;
}

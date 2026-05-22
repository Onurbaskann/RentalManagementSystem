namespace KiraTakip.Models.ViewModels;

public class KiraciOdemePortalViewModel
{
    public int KiraciId { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string GosterimAdi => string.IsNullOrWhiteSpace(Soyad) ? Ad : $"{Ad} {Soyad}";
    public string Email { get; set; } = string.Empty;
    public List<BorcKart> Borclar { get; set; } = [];
    public int DefaultSelectedId { get; set; }
}

public class BorcKart
{
    public int TahakkukId { get; set; }
    public string TasinmazAdi { get; set; } = string.Empty;
    public string BirimAdi { get; set; } = string.Empty;
    public DateTime DonemBaslangic { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar => ToplamTutar - OdenenTutar;
}

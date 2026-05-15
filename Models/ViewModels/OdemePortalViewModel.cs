namespace KiraTakip.Models.ViewModels;

public class OdemePortalViewModel
{
    public int TahakkukId { get; set; }
    public string KiraciAdi { get; set; } = "";
    public string TasinmazAdi { get; set; } = "";
    public string BirimAdi { get; set; } = "";
    public DateTime DonemBaslangic { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal ToplamTutar { get; set; }
    public decimal OdenenTutar { get; set; }
    public decimal KalanTutar => ToplamTutar - OdenenTutar;
}
